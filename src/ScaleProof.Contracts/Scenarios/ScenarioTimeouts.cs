using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>Scenario v1 wire data; schema and semantic validation are separate boundaries.</summary>
public sealed record ScenarioTimeouts
{
    [JsonPropertyName("request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Request { get; init; }

    [JsonPropertyName("gracefulStop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GracefulStop { get; init; }
}
