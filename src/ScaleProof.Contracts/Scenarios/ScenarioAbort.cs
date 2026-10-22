using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>Scenario v1 wire data; schema and semantic validation are separate boundaries.</summary>
public sealed record ScenarioAbort
{
    [JsonPropertyName("metric")]
    public required string Metric { get; init; }

    [JsonPropertyName("aggregation")]
    public required ScenarioAggregation Aggregation { get; init; }

    [JsonPropertyName("operator")]
    public required ScenarioOperator Operator { get; init; }

    [JsonPropertyName("value")]
    public required JsonElement Value { get; init; }

    [JsonPropertyName("unit")]
    public required ScenarioUnit Unit { get; init; }

    [JsonPropertyName("request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Request { get; init; }

    [JsonPropertyName("over")]
    public required string Over { get; init; }
}
