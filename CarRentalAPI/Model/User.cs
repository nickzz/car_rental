public class User
{
    public int Id { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string? Role { get; set; } // "Customer", "Owner"
}
