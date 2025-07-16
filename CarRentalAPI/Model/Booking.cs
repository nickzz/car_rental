public class Booking
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public Car? Car { get; set; }

    public int CustomerId { get; set; }
    public User? Customer { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Rejected"
}
