namespace HotelBookingAPI.Tests;

/// <summary>Marks a test that requires a local Docker daemon for Testcontainers.</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("HOTELBOOKING_RUN_POSTGRES_TESTS"),
            "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set HOTELBOOKING_RUN_POSTGRES_TESTS=true to run PostgreSQL Testcontainers tests (Docker required).";
        }
    }
}
