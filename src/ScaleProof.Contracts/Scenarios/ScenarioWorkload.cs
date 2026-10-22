using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>Scenario v1 wire data; schema and semantic validation are separate boundaries.</summary>
public sealed record ScenarioWorkload
{
    [JsonPropertyName("engine")]
    public required ScenarioEngine Engine { get; init; }

    [JsonPropertyName("profile")]
    public required ScenarioProfile Profile { get; init; }

    [JsonPropertyName("iterations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Iterations { get; init; }

    [JsonPropertyName("vus")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Vus { get; init; }

    [JsonPropertyName("rate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Rate { get; init; }

    [JsonPropertyName("timeUnit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TimeUnit { get; init; }

    [JsonPropertyName("preAllocatedVUs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? PreAllocatedVUs { get; init; }

    [JsonPropertyName("maxVUs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? MaxVUs { get; init; }

    [JsonPropertyName("stages")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScenarioStage[]? Stages { get; init; }

    [JsonPropertyName("windows")]
    public required ScenarioWindows Windows { get; init; }

    [JsonPropertyName("timeouts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScenarioTimeouts? Timeouts { get; init; }

    [JsonPropertyName("limits")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScenarioLimits? Limits { get; init; }
}
