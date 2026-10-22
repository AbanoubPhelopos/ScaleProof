using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>Scenario v1 wire data; schema and semantic validation are separate boundaries.</summary>
public sealed record ScenarioV1
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("target")]
    public required ScenarioTarget Target { get; init; }

    [JsonPropertyName("workload")]
    public required ScenarioWorkload Workload { get; init; }

    [JsonPropertyName("requests")]
    public required ScenarioRequest[] Requests { get; init; }

    [JsonPropertyName("thresholds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScenarioThreshold[]? Thresholds { get; init; }

    [JsonPropertyName("abort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScenarioAbort[]? Abort { get; init; }

    [JsonPropertyName("checks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScenarioCheck[]? Checks { get; init; }

    [JsonPropertyName("classification")]
    public required ScenarioClassification Classification { get; init; }

    [JsonPropertyName("capture")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScenarioCapture? Capture { get; init; }

    [JsonPropertyName("report")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScenarioReport? Report { get; init; }
}
