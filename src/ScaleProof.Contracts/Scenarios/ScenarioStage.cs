using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>Scenario v1 wire data; schema and semantic validation are separate boundaries.</summary>
public sealed record ScenarioStage
{
    [JsonPropertyName("rate")]
    public required long Rate { get; init; }

    [JsonPropertyName("duration")]
    public required string Duration { get; init; }
}
