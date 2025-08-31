
public enum UserRole { Customer, Admin }

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DOB { get; set; }
    public string ICNumber { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string PasswordHash { get; set; }
    public string PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

