using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>A string, number, boolean, or dataset selection. Numeric tokens retain their precision.</summary>
[JsonConverter(typeof(ScenarioParameterValueConverter))]
public abstract record ScenarioParameterValue
{
    public sealed record Literal(JsonElement Value) : ScenarioParameterValue;
    public sealed record Dataset(ScenarioDatasetPick Selection) : ScenarioParameterValue;
}

public sealed class ScenarioParameterValueConverter : JsonConverter<ScenarioParameterValue>
{
    public override bool HandleNull => true;

    public override ScenarioParameterValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return new ScenarioParameterValue.Dataset(
                JsonSerializer.Deserialize(ref reader, ScenarioJsonContext.Default.ScenarioDatasetPick)!);
        }

        if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.Number or JsonTokenType.True or JsonTokenType.False))
        {
            throw new JsonException("Expected a scalar parameter or dataset selection.");
        }

        using var document = JsonDocument.ParseValue(ref reader);
        return new ScenarioParameterValue.Literal(document.RootElement.Clone());
    }

    public override void Write(Utf8JsonWriter writer, ScenarioParameterValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case ScenarioParameterValue.Literal literal when literal.Value.ValueKind is
                JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False:
                literal.Value.WriteTo(writer);
                break;
            case ScenarioParameterValue.Dataset dataset:
                JsonSerializer.Serialize(writer, dataset.Selection, ScenarioJsonContext.Default.ScenarioDatasetPick);
                break;
            default:
                throw new JsonException("Unsupported parameter value.");
        }
    }
}
