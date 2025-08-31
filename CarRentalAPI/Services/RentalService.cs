using Microsoft.EntityFrameworkCore;

public class RentalService
{
    private readonly ApplicationDbContext _context;

    public RentalService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SubmitApplication(BookingDto dto, int userId)
    {
        var startUtc = DateTime.SpecifyKind(dto.StartDate.Date, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(dto.EndDate.Date, DateTimeKind.Utc);

        if (endUtc <= startUtc)
            throw new ArgumentException("End date must be after start date");

        var car = await _context.Cars.FindAsync(dto.CarId);
        if (car == null)
            throw new ArgumentException("Car not found");

        var totalDays = (endUtc - startUtc).Days + 1; // inclusive of last day
        if (totalDays <= 0)
            throw new ArgumentException("Invalid rental duration");

        // 🔹 Price breakdown
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
    }


    public async Task<IEnumerable<Booking>> GetUserApplications(int userId)
    {
        return await _context.Bookings
            .Include(r => r.Car)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetPendingApplications()
    {
        return await _context.Bookings
            .Include(r => r.User)
            .Include(r => r.Car)
            .Where(r => r.Status == ApplicationStatus.Pending)
            .ToListAsync();
    }

    public async Task ApproveApplication(int id)
    {
        var app = await _context.Bookings.FindAsync(id);
        if (app != null)
        {
            app.Status = ApplicationStatus.Approved;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RejectApplication(int id)
    {
        var app = await _context.Bookings.FindAsync(id);
        if (app != null)
        {
            app.Status = ApplicationStatus.Rejected;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SendMessage(int id, string message)
    {
        var app = await _context.Bookings.FindAsync(id);
        if (app != null)
        {
            app.MessageToCustomer = message;
            await _context.SaveChangesAsync();
        }
    }
}
