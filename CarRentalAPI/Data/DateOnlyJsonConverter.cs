using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Date must be a string.");

        var value = reader.GetString();
        if (value is null)
            throw new JsonException("A date is required.");

        if (DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        // Accept ISO timestamps for compatibility, but keep the supplied calendar date instead of shifting by its offset.
        if (value.Length > DateFormat.Length &&
            value[10] == 'T' &&
            DateOnly.TryParseExact(value[..DateFormat.Length], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) &&
            DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return date;
        }

        throw new JsonException("Date must be YYYY-MM-DD or an ISO 8601 timestamp.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(DateFormat, CultureInfo.InvariantCulture));
}
