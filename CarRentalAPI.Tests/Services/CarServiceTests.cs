using CarRentalAPI.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace CarRentalAPI.Tests.Services;

[Collection(CarRentalAPI.Tests.Integration.PostgresTestDatabaseCollection.Name)]
public sealed class CarServiceTests(CarRentalAPI.Tests.Integration.PostgresTestDatabase database)
{
    [Fact]
    public async Task Availability_AllowsAdjacentHalfOpenDateRanges()
    {
        var (car, _) = await CreateBooking(BookingStatus.Confirmed, new DateOnly(2035, 5, 1), new DateOnly(2035, 5, 4));
        await using var context = database.CreateContext();
        var service = new CarService(context);

        var available = await service.GetAvailableCarsAsync(new DateOnly(2035, 5, 4), new DateOnly(2035, 5, 6));

        Assert.Contains(available, item => item.Id == car.Id);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed, false)]
    [InlineData(BookingStatus.Cancelled, true)]
    [InlineData(BookingStatus.Rejected, true)]
    [InlineData(BookingStatus.Completed, true)]
    public async Task Availability_FiltersByBlockingStatus(BookingStatus status, bool expectedAvailable)
    {
        var (car, _) = await CreateBooking(status, new DateOnly(2035, 6, 1), new DateOnly(2035, 6, 8));
        await using var context = database.CreateContext();
        var available = await new CarService(context).GetAvailableCarsAsync(
            new DateOnly(2035, 6, 1), new DateOnly(2035, 6, 8));

        Assert.Equal(expectedAvailable, available.Any(item => item.Id == car.Id));
    }

    [Fact]
    public async Task Availability_ContainedAndIdenticalRangesAreUnavailable()
    {
        var (car, _) = await CreateBooking(BookingStatus.Pending, new DateOnly(2035, 7, 1), new DateOnly(2035, 7, 10));
        await using var context = database.CreateContext();
        var service = new CarService(context);

        var contained = await service.GetAvailableCarsAsync(new DateOnly(2035, 7, 3), new DateOnly(2035, 7, 5));
        var identical = await service.GetAvailableCarsAsync(new DateOnly(2035, 7, 1), new DateOnly(2035, 7, 10));

        Assert.DoesNotContain(contained, item => item.Id == car.Id);
        Assert.DoesNotContain(identical, item => item.Id == car.Id);
    }

    [Fact]
    public async Task SubmitBooking_RejectsInvalidUrlBeforeDatabaseOverlapQuery()
    {
        await using var context = database.CreateContext();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Booking:MaximumRentalDays"] = "365" })
            .Build();
        var service = new RentalService(context, NullLogger<RentalService>.Instance, configuration);
        var tomorrow = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Kuala_Lumpur")).AddDays(1);

        await Assert.ThrowsAsync<BadRequestException>(() => service.SubmitBooking(new BookingDto
        {
            CarId = int.MaxValue,
            StartDate = tomorrow,
            EndDate = tomorrow.AddDays(1),
            DriverLicenseUrl = "file:///private/license.png"
        }, 1));
    }

    [Fact]
    public async Task SubmitBooking_PersistsTheDateFromAnOffsetTimestampWithoutShiftingIt()
    {
        var (car, user) = await CreateCarAndUser();
        var dto = JsonSerializer.Deserialize<BookingDto>(
            """{"carId":1,"startDate":"2035-04-05T00:00:00+08:00","endDate":"2035-04-06T00:00:00+08:00"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        dto.CarId = car.Id;

        await using var context = database.CreateContext();
        var configuration = new ConfigurationBuilder().Build();
        var service = new RentalService(context, NullLogger<RentalService>.Instance, configuration);
        var response = await service.SubmitBooking(dto, user.Id);

        await using var verifyContext = database.CreateContext();
        var stored = await verifyContext.Bookings.FindAsync(response.Id);
        Assert.Equal(new DateOnly(2035, 4, 5), DateOnly.FromDateTime(stored!.StartDate));
    }

    [Fact]
    public async Task BookingResponses_ContainOnlyApprovedCustomerFields()
    {
        var (_, booking) = await CreateBooking(BookingStatus.Pending,
            new DateOnly(2035, 8, 1), new DateOnly(2035, 8, 3));
        await using var context = database.CreateContext();
        var configuration = new ConfigurationBuilder().Build();
        var service = new RentalService(context, NullLogger<RentalService>.Instance, configuration);

        var customerResponse = Assert.Single(await service.GetUserApplications(booking.UserId));
        var adminResponse = (await service.GetPendingApplications()).Single(item => item.Id == booking.Id);

        Assert.DoesNotContain("password", JsonSerializer.Serialize(customerResponse), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", JsonSerializer.Serialize(adminResponse), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Test", adminResponse.CustomerFirstName);
        Assert.NotNull(adminResponse.CustomerEmail);
    }

    private async Task<(Car Car, Booking Booking)> CreateBooking(BookingStatus status, DateOnly start, DateOnly end)
    {
        var (car, user) = await CreateCarAndUser();
        await using var context = database.CreateContext();
        var booking = new Booking
        {
            UserId = user.Id, CarId = car.Id, StartDate = start.ToDateTime(TimeOnly.MinValue),
            EndDate = end.ToDateTime(TimeOnly.MinValue), TotalPrice = 100, Status = status
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        return (car, booking);
    }

    private async Task<(Car Car, User User)> CreateCarAndUser()
    {
        await using var context = database.CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var user = new User
        {
            FirstName = "Test", LastName = "Customer", DOB = new DateTime(1990, 1, 1),
            ICNumber = suffix, Email = $"{suffix}@test.invalid", Address = "Test address",
            Password = "test-only", PhoneNumber = "0123456789", Role = UserRole.Customer
        };
        var car = new Car
        {
            Brand = "Test", Model = "Car", Type = "Sedan", PlateNo = suffix[..12],
            PricePerDay = 100, PricePerWeek = 600, PricePerMonth = 2000
        };
        context.AddRange(user, car);
        await context.SaveChangesAsync();
        return (car, user);
    }
}
