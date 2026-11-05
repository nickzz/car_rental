public sealed record BookingResponseDto(
    int Id,
    int CarId,
    string? CarBrand,
    string? CarModel,
    BookingStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalPrice,
    DateTime CreatedAt);

public sealed record AdminBookingResponseDto(
    int Id,
    int CarId,
    string? CarBrand,
    string? CarModel,
    BookingStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalPrice,
    DateTime CreatedAt,
    string? CustomerFirstName,
    string? CustomerLastName,
    string? CustomerPhoneNumber,
    string? CustomerEmail);
