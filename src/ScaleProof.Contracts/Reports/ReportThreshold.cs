using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportThreshold
{
    [JsonPropertyName("metric")]
    public required string Metric { get; init; }

    [JsonPropertyName("aggregation")]
    public required string Aggregation { get; init; }

    [JsonPropertyName("operator")]
    public required string Operator { get; init; }

    [JsonPropertyName("value")]
    public required JsonElement Value { get; init; }

    [JsonPropertyName("unit")]
    public required string Unit { get; init; }

    [JsonPropertyName("observed")]
    public required JsonElement Observed { get; init; }

    [JsonPropertyName("status")]
    public required ReportThresholdStatus Status { get; init; }

    [JsonPropertyName("request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Request { get; init; }
}
