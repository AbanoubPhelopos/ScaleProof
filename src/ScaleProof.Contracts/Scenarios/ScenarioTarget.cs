using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>Scenario v1 wire data; schema and semantic validation are separate boundaries.</summary>
public sealed record ScenarioTarget
{
    [JsonPropertyName("profile")]
    public required string Profile { get; init; }

    [JsonPropertyName("basePath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BasePath { get; init; }
}
