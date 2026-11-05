using CarRentalAPI.Exceptions;
using Microsoft.EntityFrameworkCore;

public class CarService
{
    private readonly ApplicationDbContext _context;

    public CarService(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<Car>> GetAllCars() =>
        await _context.Cars.AsNoTracking().ToListAsync();

    public async Task<List<Car>> GetAvailableCarsAsync(DateOnly startDate, DateOnly endDate)
    {
        var start = startDate.ToDateTime(TimeOnly.MinValue);
        var end = endDate.ToDateTime(TimeOnly.MinValue);

        return await _context.Cars
            .AsNoTracking()
            .Where(car => !_context.Bookings.Any(booking =>
                booking.CarId == car.Id &&
                BookingStatusRules.BlockingStatuses.Contains(booking.Status) &&
                booking.StartDate < end && start < booking.EndDate))
            .ToListAsync();
    }

    public async Task<decimal?> GetEstimatedPriceAsync(int carId, DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
            return null;

        var car = await _context.Cars.FindAsync(carId);
        return car is null ? null : RentalPricing.Calculate(car, startDate, endDate);
    }

    public async Task<Car?> GetCarById(int id) => await _context.Cars.FindAsync(id);

    public async Task AddCar(CarDto dto)
    {
        _context.Cars.Add(dto.ToEntity());
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCar(int id, CarDto updatedCar)
    {
        var car = await _context.Cars.FindAsync(id)
            ?? throw new NotFoundException($"Car with ID {id} not found");

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
        var hasActiveBookings = await _context.Bookings.AnyAsync(booking =>
            booking.CarId == id &&
            BookingStatusRules.BlockingStatuses.Contains(booking.Status) &&
            booking.StartDate <= today && today < booking.EndDate);

        if (hasActiveBookings)
            throw new ConflictException("A car with an active booking cannot be updated.");

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
        var car = await _context.Cars.FindAsync(id)
            ?? throw new NotFoundException($"Car with ID {id} not found");

        var hasBlockingBookings = await _context.Bookings.AnyAsync(booking =>
            booking.CarId == id && BookingStatusRules.BlockingStatuses.Contains(booking.Status));

        if (hasBlockingBookings)
            throw new ConflictException("A car with a blocking booking cannot be deleted.");

        var hasBookingHistory = await _context.Bookings.AnyAsync(booking => booking.CarId == id);
        if (hasBookingHistory)
            throw new ConflictException("A car with booking history cannot be deleted.");

        _context.Cars.Remove(car);
        await _context.SaveChangesAsync();
    }
}
