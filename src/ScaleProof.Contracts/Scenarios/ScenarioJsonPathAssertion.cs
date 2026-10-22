using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>Scenario v1 wire data; schema and semantic validation are separate boundaries.</summary>
public sealed record ScenarioJsonPathAssertion
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("exists")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Exists { get; init; }

    [JsonPropertyName("equals")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement ExpectedValue { get; init; }
}
