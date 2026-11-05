public static class RentalPricing
{
    private static readonly (int Days, Func<Car, decimal> Price)[] Tiers =
    [
        (1, car => car.PricePerDay),
        (7, car => car.PricePerWeek),
        (30, car => car.PricePerMonth)
    ];

    /// <summary>Calculates the lowest tier combination that covers a half-open rental period [start, end).</summary>
    public static decimal Calculate(Car car, DateOnly start, DateOnly end)
    {
        var rentalDays = end.DayNumber - start.DayNumber;
        if (rentalDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(end), "End date must be after start date.");

        var prices = new decimal[rentalDays + 1];
        for (var coveredDays = 1; coveredDays <= rentalDays; coveredDays++)
        {
            prices[coveredDays] = Tiers
                .Select(tier => prices[Math.Max(0, coveredDays - tier.Days)] + tier.Price(car))
                .Min();
        }

        return prices[rentalDays];
    }
}
