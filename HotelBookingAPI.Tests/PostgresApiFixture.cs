using HotelBookingAPI.Data;
using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HotelBookingAPI.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresApiFixture>
{
    public const string Name = "PostgreSQL API integration";
}

public sealed class PostgresApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private TestApiFactory? _factory;

    public bool IsEnabled => string.Equals(
        Environment.GetEnvironmentVariable("HOTELBOOKING_RUN_POSTGRES_TESTS"),
        "true", StringComparison.OrdinalIgnoreCase);

    public HttpClient CreateClient() => _factory!.CreateClient();
    public IServiceScope CreateScope() => _factory!.Services.CreateScope();

    public async Task InitializeAsync()
    {
        if (!IsEnabled) return;

        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("hotelbooking_tests")
            .WithUsername("postgres")
            .WithPassword("test-password")
            .Build();
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).Options;
        await using (var db = new AppDbContext(options))
            await db.Database.EnsureCreatedAsync();

        _factory = new TestApiFactory(_postgres.GetConnectionString());
        _ = _factory.Services; // Starts the host and runs the seeders.
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        if (_postgres is not null) await _postgres.DisposeAsync();
    }

    public async Task<string> CreateCustomerAsync()
    {
        using var scope = CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var email = $"customer-{Guid.NewGuid():N}@example.test";
        var user = new ApplicationUser { FullName = "Integration Customer", UserName = email, Email = email, EmailConfirmed = true };
        var result = await manager.CreateAsync(user, "TestPass123!");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        await manager.AddToRoleAsync(user, "Customer");
        return user.Id;
    }

    private sealed class TestApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["Jwt:Issuer"] = "HotelBookingAPI.Tests",
                    ["Jwt:Audience"] = "HotelBookingAPI.Tests",
                    ["Jwt:Key"] = "TestOnlySigningKey_32_Bytes_Minimum_12345",
                    ["AdminSeed:Email"] = "test-admin@example.test",
                    ["AdminSeed:Password"] = "TestAdmin123!"
                }));
        }
    }
}
