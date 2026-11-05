public static class BookingStatusRules
{
    public static BookingStatus[] BlockingStatuses { get; } =
    [
        BookingStatus.Pending,
        BookingStatus.DocumentsVerified,
        BookingStatus.Approved,
        BookingStatus.Confirmed,
        BookingStatus.Active
    ];

    public static bool IsBlocking(BookingStatus status) => BlockingStatuses.Contains(status);

    public static bool TryParse(string? value, out BookingStatus status)
    {
        if (Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status))
            return true;

        status = default;
        return false;
    }

    public static bool CanTransitionTo(BookingStatus current, BookingStatus next) =>
        current switch
        {
            BookingStatus.Pending => next is BookingStatus.DocumentsVerified or BookingStatus.Rejected or BookingStatus.Cancelled,
            BookingStatus.DocumentsVerified => next is BookingStatus.Approved or BookingStatus.Rejected or BookingStatus.Cancelled,
            BookingStatus.Approved => next is BookingStatus.Confirmed or BookingStatus.Rejected or BookingStatus.Cancelled,
            BookingStatus.Confirmed => next is BookingStatus.Active or BookingStatus.Cancelled,
            BookingStatus.Active => next is BookingStatus.Completed,
            _ => false
        };
}
