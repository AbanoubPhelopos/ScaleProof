using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Rejects unsupported versions at the report reader/writer boundary.</summary>
public sealed class ReportSchemaVersionConverter : JsonConverter<string>
{
    private const string ErrorMessage = "Unsupported report schema version.";

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || reader.GetString() != ReportV1.SupportedSchemaVersion)
        {
            throw new JsonException(ErrorMessage);
        }

        return ReportV1.SupportedSchemaVersion;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (value != ReportV1.SupportedSchemaVersion)
        {
            throw new JsonException(ErrorMessage);
        }

        writer.WriteStringValue(value);
    }
}
