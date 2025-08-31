using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        string? connectionString;

        // Check if DATABASE_URL is set (production)
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrEmpty(databaseUrl))
        {
            var databaseUri = new Uri(databaseUrl);
            var userInfo = databaseUri.UserInfo.Split(':');
            var port = databaseUri.Port != -1 ? databaseUri.Port : 5432;

            connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = databaseUri.Host,
                Port = port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/'),
                SslMode = SslMode.Require
            }.ToString();
        }
        else
        {
            // Local fallback → read from appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            connectionString = config.GetConnectionString("DefaultConnection");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
