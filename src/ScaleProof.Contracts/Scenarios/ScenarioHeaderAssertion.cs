using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>Scenario v1 wire data; schema and semantic validation are separate boundaries.</summary>
public sealed record ScenarioHeaderAssertion
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("equals")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExpectedValue { get; init; }

    [JsonPropertyName("contains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Contains { get; init; }
}
