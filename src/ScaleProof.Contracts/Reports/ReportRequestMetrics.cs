using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportRequestMetrics
{
    [JsonPropertyName("request")]
    public required string Request { get; init; }

    [JsonPropertyName("metrics")]
    public required ReportMetric[] Metrics { get; init; }
}
