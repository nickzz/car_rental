using Microsoft.EntityFrameworkCore;

public class CarService
{
    private readonly ApplicationDbContext _context;

    public CarService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Car>> GetAllCars()
    {
        return await _context.Cars.ToListAsync();
    }

    public async Task<List<Car>> GetAvailableCarsAsync(DateTime startDate, DateTime endDate)
    {
        startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

        var unavailableCarIds = await _context.Bookings
            .Where(b =>
                // Overlapping logic
                !(b.EndDate < startDate || b.StartDate > endDate)
            )
            .Select(b => b.CarId)
            .Distinct()
            .ToListAsync();

        var availableCars = await _context.Cars
            .Where(c => !unavailableCarIds.Contains(c.Id))
            .ToListAsync();

        return availableCars;
    }

    public async Task<decimal?> GetEstimatedPriceAsync(int carId, DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate)
            return null; // invalid

        var car = await _context.Cars.FindAsync(carId);
        if (car == null)
            return null; // car not found

        var totalDays = (endDate - startDate).Days;
        if (totalDays <= 0)
            return null;

        decimal totalPrice;

        if (totalDays >= 30)
        {
            var months = totalDays / 30;
            var remainingDays = totalDays % 30;
            totalPrice = (months * car.PricePerMonth) + (remainingDays * car.PricePerDay);
        }
        else if (totalDays >= 7)
        {
            var weeks = totalDays / 7;
            var remainingDays = totalDays % 7;
            totalPrice = (weeks * car.PricePerWeek) + (remainingDays * car.PricePerDay);
        }
        else
        {
            totalPrice = totalDays * car.PricePerDay;
        }

        return totalPrice;
    }

    public async Task<Car> GetCarById(int id)
    {
        return await _context.Cars.FindAsync(id);
    }

    public async Task AddCar(Car car)
    {
        _context.Cars.Add(car);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCar(int id, Car updatedCar)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return;

        // Check if car has active bookings
        var hasActiveBookings = await _context.Bookings
            .Where(b => b.CarId == id && b.StartDate <= DateTime.UtcNow && b.EndDate >= DateTime.UtcNow)
            .AnyAsync();

        if (hasActiveBookings)
            return; // Cannot update car with active bookings

        car.Brand = updatedCar.Brand;
        car.Type = updatedCar.Type;
        car.PlateNo = updatedCar.PlateNo;
        car.Colour = updatedCar.Colour;
        car.Model = updatedCar.Model;
        car.PricePerDay = updatedCar.PricePerDay;
        car.PricePerWeek = updatedCar.PricePerWeek;
        car.PricePerMonth = updatedCar.PricePerMonth;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteCar(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return;

        // Check if car has any bookings
        var hasBookings = await _context.Bookings
            .Where(b => b.CarId == id)
            .AnyAsync();

        if (hasBookings)
            return; // Cannot delete car with existing bookings

        _context.Cars.Remove(car);
        await _context.SaveChangesAsync();
    }
}
