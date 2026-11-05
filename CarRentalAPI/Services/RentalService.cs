using CarRentalAPI.Exceptions;
using Microsoft.EntityFrameworkCore;

public class RentalService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RentalService> _logger;
    private readonly IConfiguration _configuration;

    public RentalService(ApplicationDbContext context, ILogger<RentalService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<decimal> CalculateFinalPrice(int carId, DateOnly start, DateOnly end)
    {
        var car = await _context.Cars.FindAsync(carId)
            ?? throw new NotFoundException("Car not found");

        return RentalPricing.Calculate(car, start, end);
    }

    public async Task<BookingResponseDto> SubmitBooking(BookingDto dto, int userId)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            DateTime.UtcNow, "Asia/Kuala_Lumpur"));
        var maximumRentalDays = _configuration.GetValue("Booking:MaximumRentalDays", 365);

        if (dto.StartDate < today)
            throw new BadRequestException("Booking start date cannot be in the past.");
        if (dto.EndDate <= dto.StartDate)
            throw new BadRequestException("Booking end date must be after the start date.");
        if (dto.EndDate.DayNumber - dto.StartDate.DayNumber > maximumRentalDays)
            throw new BadRequestException($"Rental length cannot exceed {maximumRentalDays} days.");
        if (dto.DriverLicenseUrl is { Length: > 2048 } ||
            (dto.DriverLicenseUrl is not null &&
             (!Uri.TryCreate(dto.DriverLicenseUrl, UriKind.Absolute, out var licenseUri) ||
              (licenseUri.Scheme != Uri.UriSchemeHttp && licenseUri.Scheme != Uri.UriSchemeHttps))))
        {
            throw new BadRequestException("DriverLicenseUrl must be a valid HTTP or HTTPS URL no longer than 2048 characters.");
        }

        var car = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == dto.CarId)
            ?? throw new NotFoundException("Car not found");

        var start = dto.StartDate.ToDateTime(TimeOnly.MinValue);
        var end = dto.EndDate.ToDateTime(TimeOnly.MinValue);
        var isBusy = await _context.Bookings.AnyAsync(b =>
            b.CarId == dto.CarId &&
            BookingStatusRules.BlockingStatuses.Contains(b.Status) &&
            b.StartDate < end && start < b.EndDate);

        if (isBusy)
            throw new ConflictException("Car was booked just now.");

        var booking = new Booking
        {
            UserId = userId,
            CarId = dto.CarId,
            StartDate = start,
            EndDate = end,
            TotalPrice = RentalPricing.Calculate(car, dto.StartDate, dto.EndDate),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return new BookingResponseDto(
            booking.Id, booking.CarId, car.Brand, car.Model, booking.Status,
            dto.StartDate, dto.EndDate, booking.TotalPrice, booking.CreatedAt);
    }

    public async Task<IEnumerable<BookingResponseDto>> GetUserApplications(int userId)
    {
        var bookings = await _context.Bookings
            .Where(booking => booking.UserId == userId)
            .OrderByDescending(booking => booking.CreatedAt)
            .AsNoTracking()
            .Select(booking => new
            {
                booking.Id,
                booking.CarId,
                booking.Car.Brand,
                booking.Car.Model,
                booking.Status,
                booking.StartDate,
                booking.EndDate,
                booking.TotalPrice,
                booking.CreatedAt
            })
            .ToListAsync();

        return bookings.Select(booking => new BookingResponseDto(
            booking.Id, booking.CarId, booking.Brand, booking.Model, booking.Status,
            DateOnly.FromDateTime(booking.StartDate), DateOnly.FromDateTime(booking.EndDate),
            booking.TotalPrice, booking.CreatedAt));
    }

    public async Task<IEnumerable<AdminBookingResponseDto>> GetPendingApplications()
    {
        var bookings = await _context.Bookings
            .Where(booking => booking.Status == BookingStatus.Pending)
            .OrderBy(booking => booking.CreatedAt)
            .AsNoTracking()
            .Select(booking => new
            {
                booking.Id,
                booking.CarId,
                booking.Car.Brand,
                booking.Car.Model,
                booking.Status,
                booking.StartDate,
                booking.EndDate,
                booking.TotalPrice,
                booking.CreatedAt,
                booking.User.FirstName,
                booking.User.LastName,
                booking.User.PhoneNumber,
                booking.User.Email
            })
            .ToListAsync();

        return bookings.Select(booking => new AdminBookingResponseDto(
            booking.Id, booking.CarId, booking.Brand, booking.Model, booking.Status,
            DateOnly.FromDateTime(booking.StartDate), DateOnly.FromDateTime(booking.EndDate),
            booking.TotalPrice, booking.CreatedAt, booking.FirstName, booking.LastName,
            booking.PhoneNumber, booking.Email));
    }

    public async Task UpdateApplicationStatus(int applicationId, string statusString, string? messageToCustomer)
    {
        if (!BookingStatusRules.TryParse(statusString, out var status))
            throw new BadRequestException($"Invalid application status: {statusString}");

        var application = await _context.Bookings.FindAsync(applicationId)
            ?? throw new NotFoundException($"Booking with ID {applicationId} not found");

        if (!BookingStatusRules.CanTransitionTo(application.Status, status))
            throw new ConflictException($"Booking cannot transition from {application.Status} to {status}.");

        if (BookingStatusRules.IsBlocking(status))
        {
            var overlaps = await _context.Bookings.AnyAsync(booking =>
                booking.Id != application.Id &&
                booking.CarId == application.CarId &&
                BookingStatusRules.BlockingStatuses.Contains(booking.Status) &&
                booking.StartDate < application.EndDate && application.StartDate < booking.EndDate);

            if (overlaps)
                throw new ConflictException("The car is no longer available for these dates.");
        }

        application.Status = status;
        application.AdminMessage = messageToCustomer;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {BookingId} status updated to {Status}", applicationId, status);
    }
}
