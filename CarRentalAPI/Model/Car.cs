public class Car
{
    public int Id { get; set; }
    public string? Model { get; set; }
    public string? Description { get; set; }
    public decimal PricePerDay { get; set; }
    public bool IsAvailable { get; set; } = true;

    public int OwnerId { get; set; }
    public User? Owner { get; set; }
}
