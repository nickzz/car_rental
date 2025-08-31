
public enum ApplicationStatus { Pending, Approved, Rejected }
public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int CarId { get; set; }
    public Car? Car { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ApplicationStatus Status { get; set; }
    public string? MessageToCustomer { get; set; }
    public string? Notes { get; set; }
    public decimal EstimatedPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}
