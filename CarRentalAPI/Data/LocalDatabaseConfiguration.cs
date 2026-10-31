using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CarRentalAPI.Data;

public static class LocalDatabaseConfiguration
{
    public static string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        var settings = new NpgsqlConnectionStringBuilder(connectionString);

        if (!string.Equals(settings.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || settings.Port != 5432
            || !string.Equals(settings.Database, "car_rentalDB", StringComparison.Ordinal)
            || !string.Equals(settings.Username, "postgres", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "DefaultConnection must target localhost:5432/car_rentalDB as user postgres.");
        }

        return connectionString;
    }
}
