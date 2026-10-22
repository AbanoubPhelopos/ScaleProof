using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>Scenario v1 wire data; schema and semantic validation are separate boundaries.</summary>
public sealed record ScenarioWindows
{
    [JsonPropertyName("warmUp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WarmUp { get; init; }

    [JsonPropertyName("measurement")]
    public required string Measurement { get; init; }

    [JsonPropertyName("rampDown")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RampDown { get; init; }
}
