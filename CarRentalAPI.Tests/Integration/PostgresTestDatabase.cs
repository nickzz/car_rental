using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace CarRentalAPI.Tests.Integration;

public sealed class PostgresTestDatabase : IAsyncLifetime
{
    private const string TestDatabaseName = "car_rental_test_db";
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var source = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection in local .NET User Secrets.");
        var local = new NpgsqlConnectionStringBuilder(source);
        if (!string.Equals(local.Host, "localhost", StringComparison.OrdinalIgnoreCase) || local.Port != 5432)
            throw new InvalidOperationException("Integration tests require PostgreSQL at localhost:5432.");

        var adminConnection = new NpgsqlConnectionStringBuilder(local.ConnectionString) { Database = "postgres" };
        await using (var connection = new NpgsqlConnection(adminConnection.ConnectionString))
        {
            await connection.OpenAsync();
            await using var check = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
            check.Parameters.AddWithValue("name", TestDatabaseName);
            if (await check.ExecuteScalarAsync() is null)
            {
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{TestDatabaseName}\"", connection);
                await create.ExecuteNonQueryAsync();
            }
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(local.ConnectionString)
        {
            Database = TestDatabaseName
        }.ConnectionString;

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name)]
public sealed class PostgresTestDatabaseCollection : ICollectionFixture<PostgresTestDatabase>
{
    public const string Name = "PostgreSQL test database";
}
