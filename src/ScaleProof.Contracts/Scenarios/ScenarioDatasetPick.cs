using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>Scenario v1 wire data; schema and semantic validation are separate boundaries.</summary>
public sealed record ScenarioDatasetPick
{
    [JsonPropertyName("dataset")]
    public required string Dataset { get; init; }

    [JsonPropertyName("pick")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScenarioDatasetSelection? Pick { get; init; }
}
