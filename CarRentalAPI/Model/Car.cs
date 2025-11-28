using System.ComponentModel.DataAnnotations;

public class Car
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Brand is required")]
    [StringLength(50, ErrorMessage = "Brand name cannot exceed 50 characters")]
    public string? Brand { get; set; }

    [Required(ErrorMessage = "Model is required")]
    [StringLength(50, ErrorMessage = "Model name cannot exceed 50 characters")]
    public string? Model { get; set; }

    [Required(ErrorMessage = "Type is required")]
    [StringLength(30, ErrorMessage = "Type cannot exceed 30 characters")]
    public string? Type { get; set; }

    [Required(ErrorMessage = "Plate number is required")]
    [StringLength(20, ErrorMessage = "Plate number cannot exceed 20 characters")]
    public string? PlateNo { get; set; }

    [StringLength(30, ErrorMessage = "Colour cannot exceed 30 characters")]
    public string? Colour { get; set; }

    [Required(ErrorMessage = "Price per day is required")]
    [Range(0.01, 10000, ErrorMessage = "Price per day must be between RM 0.01 and RM 10,000")]
    public decimal PricePerDay { get; set; }

    [Required(ErrorMessage = "Price per week is required")]
    [Range(0.01, 50000, ErrorMessage = "Price per week must be between RM 0.01 and RM 50,000")]
    public decimal PricePerWeek { get; set; }

    [Required(ErrorMessage = "Price per month is required")]
    [Range(0.01, 100000, ErrorMessage = "Price per month must be between RM 0.01 and RM 100,000")]
    public decimal PricePerMonth { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}