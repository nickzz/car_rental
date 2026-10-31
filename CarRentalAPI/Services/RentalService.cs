using Microsoft.EntityFrameworkCore;
using CarRentalAPI.Exceptions;

public class RentalService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RentalService> _logger;

    public RentalService(ApplicationDbContext context, ILogger<RentalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<decimal> CalculateFinalPrice(int carId, DateTime start, DateTime end)
    {
        var car = await _context.Cars.FindAsync(carId) 
                  ?? throw new NotFoundException("Car not found");

        int totalDays = (end.Date - start.Date).Days;
        if (totalDays <= 0) throw new BadRequestException("Invalid duration");

        // Tiered Pricing Engine
        int months = totalDays / 30;
        int remainingDays = totalDays % 30;
        int weeks = remainingDays / 7;
        int days = remainingDays % 7;

        return (months * car.PricePerMonth) + 
               (weeks * car.PricePerWeek) + 
               (days * car.PricePerDay);
    }

    public async Task SubmitBooking(BookingDto dto, int userId)
    {
        var startUtc = DateTime.SpecifyKind(dto.StartDate.Date, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(dto.EndDate.Date, DateTimeKind.Utc);

        // Final availability check before save
        var isBusy = await _context.Bookings.AnyAsync(b => 
            b.CarId == dto.CarId && 
            b.Status != BookingStatus.Rejected &&
            !(b.EndDate < startUtc || b.StartDate > endUtc));

        if (isBusy) throw new ConflictException("Car was booked just now.");

        var booking = new Booking
        {
            UserId = userId,
            CarId = dto.CarId,
            StartDate = startUtc,
            EndDate = endUtc,
            TotalPrice = await CalculateFinalPrice(dto.CarId, startUtc, endUtc),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Booking>> GetUserApplications(int userId)
    {
        return await _context.Bookings
            .Include(r => r.Car)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetPendingApplications()
    {
        return await _context.Bookings
            .Include(r => r.User)
            .Include(r => r.Car)
            .Where(r => r.Status == BookingStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateApplicationStatus(int applicationId, string statusString, string? messageToCustomer)
    {
        // Parse string status to enum
        if (!Enum.TryParse<BookingStatus>(statusString, true, out var status))
            throw new BadRequestException($"Invalid application status: {statusString}");

        // Find the booking/application
        var application = await _context.Bookings.FindAsync(applicationId);
        if (application == null)
            throw new NotFoundException($"Booking with ID {applicationId} not found");

        // Update status
        application.Status = status;
        application.AdminMessage = messageToCustomer;

        // Persist status change
        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {BookingId} status updated to {Status}",
            applicationId, status);
    }
    // public async Task SendMessage(int id, string message)
    // {
    //     var app = await _context.Bookings.FindAsync(id);
    //     if (app == null)
    //         throw new NotFoundException($"Booking with ID {id} not found");

    //     app.MessageToCustomer = message;
    //     await _context.SaveChangesAsync();

    //     _logger.LogInformation("Message sent to booking {BookingId}", id);
    // }
}