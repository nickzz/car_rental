using System.Text.Json;
using Xunit;

namespace CarRentalAPI.Tests.Services;

public sealed class BookingRulesTests
{
    [Theory]
    [InlineData(1, 100)]
    [InlineData(6, 600)]
    [InlineData(7, 600)]
    [InlineData(8, 700)]
    [InlineData(29, 2000)]
    [InlineData(30, 2000)]
    [InlineData(37, 2600)]
    [InlineData(60, 4000)]
    public void Pricing_UsesCheapestCombinationOfDateBasedTiers(int days, decimal expected)
    {
        var car = new Car { PricePerDay = 100, PricePerWeek = 600, PricePerMonth = 2000 };
        var start = new DateOnly(2035, 1, 1);

        Assert.Equal(expected, RentalPricing.Calculate(car, start, start.AddDays(days)));
    }

    [Theory]
    [InlineData(BookingStatus.Pending, BookingStatus.DocumentsVerified, true)]
    [InlineData(BookingStatus.Pending, BookingStatus.Confirmed, false)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.Active, true)]
    [InlineData(BookingStatus.Active, BookingStatus.Completed, true)]
    [InlineData(BookingStatus.Completed, BookingStatus.Cancelled, false)]
    public void StatusTransitions_EnforceTheTransitionTable(BookingStatus current, BookingStatus next, bool expected) =>
        Assert.Equal(expected, BookingStatusRules.CanTransitionTo(current, next));

    [Theory]
    [InlineData("99")]
    [InlineData("999")]
    [InlineData("-1")]
    public void StatusParsing_RejectsUndefinedNumericValues(string status)
    {
        Assert.False(BookingStatusRules.TryParse(status, out _));
    }

    [Fact]
    public void DateOnlyJsonConverter_PreservesDateFromPlusEightTimestamp()
    {
        var dto = JsonSerializer.Deserialize<BookingDto>(
            """{"carId":1,"startDate":"2035-04-05T00:00:00+08:00","endDate":"2035-04-06"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(new DateOnly(2035, 4, 5), dto!.StartDate);
    }

    [Fact]
    public void CustomerBookingResponse_DoesNotContainPasswordHashField()
    {
        var response = new BookingResponseDto(1, 2, "Test", "Car", BookingStatus.Pending,
            new DateOnly(2035, 1, 1), new DateOnly(2035, 1, 2), 100, DateTime.UtcNow);

        Assert.DoesNotContain("password", JsonSerializer.Serialize(response), StringComparison.OrdinalIgnoreCase);
    }
}
