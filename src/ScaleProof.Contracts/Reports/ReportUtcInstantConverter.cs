using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report instants must be ISO date-time strings with UTC offset zero.</summary>
public sealed class ReportUtcInstantConverter : JsonConverter<DateTimeOffset>
{
    private const string ErrorMessage = "Expected a UTC report timestamp.";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || !reader.TryGetDateTimeOffset(out var instant) || instant.Offset != TimeSpan.Zero ||
            !(reader.GetString()!.EndsWith('Z') || reader.GetString()!.EndsWith("+00:00", StringComparison.Ordinal)))
        {
            throw new JsonException(ErrorMessage);
        }

        return instant;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new JsonException(ErrorMessage);
        }

        writer.WriteStringValue(value.UtcDateTime);
    }
}
