using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportMetric
{
    [JsonPropertyName("metric")]
    public required string Metric { get; init; }

    [JsonPropertyName("aggregation")]
    public required string Aggregation { get; init; }

    [JsonPropertyName("unit")]
    public required string Unit { get; init; }

    [JsonPropertyName("value")]
    public required JsonElement Value { get; init; }

    [JsonPropertyName("sampleCount")]
    public required long SampleCount { get; init; }

    [JsonPropertyName("percentileStatus")]
    [JsonConverter(typeof(ReportOptionalConverter<ReportPercentileStatus?>))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ReportOptional<ReportPercentileStatus?> PercentileStatus { get; init; }

    [JsonPropertyName("window")]
    public required ReportWindow Window { get; init; }

    [JsonPropertyName("source")]
    public required ReportMetricSource Source { get; init; }

    [JsonPropertyName("definitionVersion")]
    public required long DefinitionVersion { get; init; }

    [JsonPropertyName("unavailableReason")]
    [JsonConverter(typeof(ReportOptionalConverter<ReportUnavailableReason?>))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ReportOptional<ReportUnavailableReason?> UnavailableReason { get; init; }
}
