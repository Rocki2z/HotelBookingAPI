using System.Text;
using HotelBookingAPI.Data;
using Microsoft.EntityFrameworkCore;
using HotelBookingAPI.Interfaces;
using HotelBookingAPI.Services;
using HotelBookingAPI.Middleware;
using Microsoft.AspNetCore.Identity;
using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using HotelBookingAPI.Interfaces.Payments;
using HotelBookingAPI.Services.Payments;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;


var builder = WebApplication.CreateBuilder(args);

// Docker Compose mounts secrets as files. Convert file names such as
// Jwt__Key into normal .NET configuration keys such as Jwt:Key.
const string secretDirectory = "/run/secrets";
if (Directory.Exists(secretDirectory))
{
    var secretValues = Directory.GetFiles(secretDirectory)
        .ToDictionary(
            path => Path.GetFileName(path).Replace("__", ":"),
            path => (string?)File.ReadAllText(path).Trim());

    builder.Configuration.AddInMemoryCollection(secretValues);
}

// The container receives the PostgreSQL password as a mounted secret. Build
// the connection string here so the password never needs to be an env value.
if (string.IsNullOrWhiteSpace(
        builder.Configuration.GetConnectionString("DefaultConnection")) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Postgres:Password"]))
{
    var connectionString = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = builder.Configuration["Postgres:Host"] ?? "db",
        Port = builder.Configuration.GetValue("Postgres:Port", 5432),
        Database = builder.Configuration["Postgres:Database"] ?? "hotelbooking",
        Username = builder.Configuration["Postgres:Username"] ?? "hotelbooking",
        Password = builder.Configuration["Postgres:Password"]
    }.ConnectionString;

    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] = connectionString
    });
}

var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

var xmlPath = Path.Combine(
    AppContext.BaseDirectory,
    xmlFile
);
// Controllers
builder.Services.AddControllers();

//
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion =
        new Asp.Versioning.ApiVersion(1, 0);

    // Current routes do not include an API version segment, so accept v1
    // when callers omit the api-version query parameter.
    // Intentionally accept the existing unversioned routes as API v1.
#pragma warning disable AV0016
    options.AssumeDefaultVersionWhenUnspecified = true;
#pragma warning restore AV0016
    options.ReportApiVersions = true;
    options.ApiVersionReader = new Asp.Versioning.QueryStringApiVersionReader();
}).AddMvc();

// Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key is missing."
    );

if (System.Text.Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 bytes."
    );
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("CustomerOnly", policy =>
        policy.RequireRole("Customer"));

    options.AddPolicy("AdminOrCustomer", policy =>
        policy.RequireRole("Admin", "Customer"));
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ApiPolicy", limiter =>
    {
        limiter.PermitLimit = 100;

        limiter.Window = TimeSpan.FromMinutes(1);

        limiter.QueueLimit = 0;

        limiter.AutoReplenishment = true;
    });

    options.RejectionStatusCode = 429;
});

// Database connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
// dependency injection for services

builder.Services.AddScoped<IHotelService, HotelService>();
builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Development only. Replace with a real provider in production.
builder.Services.AddScoped<IPaymentGateway, MockPaymentGateway>();
builder.Services.AddScoped<BookingCancellationPolicy>();

// OpenAPI
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT access token."
        });

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    // Create Customer and Admin roles first.
    await IdentitySeeder.SeedRolesAsync(services);

    // Create the first Admin account.
    await AdminSeeder.SeedAdminAsync(
        services,
        app.Configuration);
}


app.UseMiddleware<RequestLoggingMiddleware>();
// Global exception handling middleware
app.UseMiddleware<GlobalExceptionMiddleware>();
// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();   
    app.MapScalarApiReference();
    app.UseHsts();
}
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the top-level entry point to WebApplicationFactory in integration tests.
public partial class Program { }
