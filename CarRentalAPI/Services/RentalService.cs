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

    public async Task SubmitApplication(BookingDto dto, int userId)
    {
        var startUtc = DateTime.SpecifyKind(dto.StartDate.Date, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(dto.EndDate.Date, DateTimeKind.Utc);

        // Validate dates
        if (endUtc <= startUtc)
            throw new BadRequestException("End date must be after start date");

        // Check if car exists
        var car = await _context.Cars.FindAsync(dto.CarId);
        if (car == null)
            throw new NotFoundException($"Car with ID {dto.CarId} not found");

        // ✅ Check if car is available for the selected dates
        var hasOverlap = await _context.Bookings
            .AnyAsync(b =>
                b.CarId == dto.CarId &&
                b.Status != ApplicationStatus.Rejected && // Rejected bookings don't block availability
                !(b.EndDate < startUtc || b.StartDate > endUtc) // Overlap logic
            );

        if (hasOverlap)
            throw new ConflictException("This car is not available for the selected dates. Please choose different dates or another car.");

        var totalDays = (endUtc - startUtc).Days;
        if (totalDays <= 0)
            throw new BadRequestException("Invalid rental duration");

        // Price breakdown
        int months = totalDays / 30;
        int remainingDaysAfterMonths = totalDays % 30;
        int weeks = remainingDaysAfterMonths / 7;
        int days = remainingDaysAfterMonths % 7;

        decimal totalPrice =
            (months * car.PricePerMonth) +
            (weeks * car.PricePerWeek) +
            (days * car.PricePerDay);

        var application = new Booking
        {
            UserId = userId,
            CarId = dto.CarId,
            StartDate = startUtc,
            EndDate = endUtc,
            Notes = dto.Notes,
            Status = ApplicationStatus.Pending,
            EstimatedPrice = totalPrice,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(application);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {BookingId} created for user {UserId} and car {CarId}",
            application.Id, userId, dto.CarId);
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
            .Where(r => r.Status == ApplicationStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateApplicationStatus(int applicationId, string statusString, string? messageToCustomer)
    {
        // Parse string status to enum
        if (!Enum.TryParse<ApplicationStatus>(statusString, true, out var status))
            throw new BadRequestException($"Invalid application status: {statusString}");

        // Find the booking/application
        var application = await _context.Bookings.FindAsync(applicationId);
        if (application == null)
            throw new NotFoundException($"Booking with ID {applicationId} not found");

        // Update status
        application.Status = status;
        application.MessageToCustomer = messageToCustomer;
        application.UpdatedAt = DateTime.UtcNow;

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