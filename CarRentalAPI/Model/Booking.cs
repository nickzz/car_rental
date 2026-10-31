
using System.ComponentModel.DataAnnotations.Schema;

public enum BookingStatus
{
    Pending,
    DocumentsVerified, // User uploaded ID
    Approved,          // Admin reviewed
    Confirmed,         // Payment received
    Active,            // Car picked up
    Completed,         // Car returned
    Rejected,
    Cancelled
}
public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CarId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalPrice { get; set; } // Server-calculated
    public BookingStatus Status { get; set; }
    public string? DriverLicenseUrl { get; set; } // Digital identity
    public string? Notes { get; set; }
    public string? AdminMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Car Car { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
