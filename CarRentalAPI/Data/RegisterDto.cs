using System.ComponentModel.DataAnnotations;

public class RegisterDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date)]
    [AgeValidation(18, ErrorMessage = "You must be at least 18 years old to register")]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "NRIC is required")]
    [RegularExpression(@"^\d{6}-\d{2}-\d{4}$", ErrorMessage = "NRIC format should be XXXXXX-XX-XXXX")]
    public string? NRIC { get; set; }

    [Required(ErrorMessage = "Address is required")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Address must be between 10 and 200 characters")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^(\+?6?01)[0-46-9]-*[0-9]{7,8}$", ErrorMessage = "Please enter a valid Malaysian phone number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    public string? Password { get; set; }
}

// Custom validation attribute for age
public class AgeValidationAttribute : ValidationAttribute
{
    private readonly int _minimumAge;

    public AgeValidationAttribute(int minimumAge)
    {
        _minimumAge = minimumAge;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;

            if (age >= _minimumAge)
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage ?? $"You must be at least {_minimumAge} years old");
        }

        return new ValidationResult("Invalid date of birth");
    }
}