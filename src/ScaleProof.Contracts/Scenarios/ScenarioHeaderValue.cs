using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>A literal header string or a secret reference, without resolving the secret.</summary>
[JsonConverter(typeof(ScenarioHeaderValueConverter))]
public abstract record ScenarioHeaderValue
{
    public sealed record Literal(string Value) : ScenarioHeaderValue;
    public sealed record Secret(ScenarioSecretReference Reference) : ScenarioHeaderValue;
}

public sealed class ScenarioHeaderValueConverter : JsonConverter<ScenarioHeaderValue>
{
    public override bool HandleNull => true;

    public override ScenarioHeaderValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => new ScenarioHeaderValue.Literal(reader.GetString()!),
            JsonTokenType.StartObject => new ScenarioHeaderValue.Secret(
                JsonSerializer.Deserialize(ref reader, ScenarioJsonContext.Default.ScenarioSecretReference)!),
            _ => throw new JsonException("Expected a header string or secret reference."),
        };

    public override void Write(Utf8JsonWriter writer, ScenarioHeaderValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case ScenarioHeaderValue.Literal literal:
                writer.WriteStringValue(literal.Value);
                break;
            case ScenarioHeaderValue.Secret secret:
                JsonSerializer.Serialize(writer, secret.Reference, ScenarioJsonContext.Default.ScenarioSecretReference);
                break;
            default:
                throw new JsonException("Unsupported header value.");
        }
    }
}
