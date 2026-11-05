using System.ComponentModel.DataAnnotations;

public class ApplicationStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? MessageToCustomer { get; set; }
}
