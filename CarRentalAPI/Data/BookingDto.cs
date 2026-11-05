using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class BookingDto
{
    [Range(1, int.MaxValue)]
    public int CarId { get; set; }

    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateOnly StartDate { get; set; }

    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateOnly EndDate { get; set; }

    [StringLength(2048)]
    public string? DriverLicenseUrl { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
