using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace CarRentalAPI.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's ApplicationDbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add ApplicationDbContext using an in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // Build the service provider
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                    // Ensure the database is created
                    db.Database.EnsureCreated();

                    // Seed test data if needed
                    SeedTestData(db);
                }
            });

            builder.UseEnvironment("Testing");
        }

        private void SeedTestData(ApplicationDbContext context)
        {
            // Add test data here if needed
            // For example:
            var adminUser = new User
            {
                FirstName = "Admin",
                LastName = "User",
                DOB = new DateTime(1990, 1, 1),
                ICNumber = "900101-01-1111",
                Email = "admin@test.com",
                Address = "Admin Address",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                PhoneNumber = "0123456789",
                Role = UserRole.Admin
            };

            var customerUser = new User
            {
                FirstName = "Customer",
                LastName = "User",
                DOB = new DateTime(1995, 5, 5),
                ICNumber = "950505-05-5555",
                Email = "customer@test.com",
                Address = "Customer Address",
                Password = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                PhoneNumber = "0987654321",
                Role = UserRole.Customer
            };

            context.Users.AddRange(adminUser, customerUser);

            var testCar = new Car
            {
                Brand = "Toyota",
                Model = "Camry",
                Type = "Sedan",
                PlateNo = "TEST123",
                Colour = "Silver",
                PricePerDay = 100,
                PricePerWeek = 600,
                PricePerMonth = 2000
            };

            context.Cars.Add(testCar);
            context.SaveChanges();
        }
    }
}