using System.ComponentModel.DataAnnotations;

public sealed class CarDto
{
    [Required, StringLength(50)]
    public string? Brand { get; set; }

    [Required, StringLength(50)]
    public string? Model { get; set; }

    [Required, StringLength(30)]
    public string? Type { get; set; }

    [Required, StringLength(20)]
    public string? PlateNo { get; set; }

    [StringLength(30)]
    public string? Colour { get; set; }

    [Range(0.01, 10000)]
    public decimal PricePerDay { get; set; }

    [Range(0.01, 50000)]
    public decimal PricePerWeek { get; set; }

    [Range(0.01, 100000)]
    public decimal PricePerMonth { get; set; }
}

public sealed record CarResponseDto(
    int Id,
    string? Brand,
    string? Model,
    string? Type,
    string? PlateNo,
    string? Colour,
    decimal PricePerDay,
    decimal PricePerWeek,
    decimal PricePerMonth,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static CarResponseDto From(Car car) => new(
        car.Id, car.Brand, car.Model, car.Type, car.PlateNo, car.Colour,
        car.PricePerDay, car.PricePerWeek, car.PricePerMonth,
        car.CreatedAt, car.UpdatedAt);
}

public static class CarDtoMapping
{
    public static Car ToEntity(this CarDto dto) => new()
    {
        Brand = dto.Brand,
        Model = dto.Model,
        Type = dto.Type,
        PlateNo = dto.PlateNo,
        Colour = dto.Colour,
        PricePerDay = dto.PricePerDay,
        PricePerWeek = dto.PricePerWeek,
        PricePerMonth = dto.PricePerMonth
    };
}
