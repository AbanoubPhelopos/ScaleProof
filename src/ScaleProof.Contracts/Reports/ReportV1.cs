using System.Text.Json;
using System.Text.Json.Serialization;
using ScaleProof.Contracts.Findings;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportV1
{
    public const string SupportedSchemaVersion = "scaleproof.report/v1";

    [JsonPropertyName("schemaVersion")]
    [JsonConverter(typeof(ReportSchemaVersionConverter))]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("reportVersion")]
    public required long ReportVersion { get; init; }

    [JsonPropertyName("generatedAt")]
    public required DateTimeOffset GeneratedAt { get; init; }

    [JsonPropertyName("tool")]
    public required ReportTool Tool { get; init; }

    [JsonPropertyName("run")]
    public required ReportRun Run { get; init; }

    [JsonPropertyName("identity")]
    public required ReportIdentity Identity { get; init; }

    [JsonPropertyName("workload")]
    public required ReportWorkload Workload { get; init; }

    [JsonPropertyName("metrics")]
    public required ReportMetric[] Metrics { get; init; }

    [JsonPropertyName("perRequest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReportRequestMetrics[]? PerRequest { get; init; }

    [JsonPropertyName("thresholds")]
    public required ReportThreshold[] Thresholds { get; init; }

    [JsonPropertyName("quality")]
    public required ReportQuality Quality { get; init; }

    [JsonPropertyName("findings")]
    public required FindingV1[] Findings { get; init; }

    [JsonPropertyName("comparison")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Comparison { get; init; }

    [JsonPropertyName("limitations")]
    public required string[] Limitations { get; init; }

    [JsonPropertyName("redaction")]
    public required ReportRedaction Redaction { get; init; }

    [JsonPropertyName("artifacts")]
    public required ReportArtifact[] Artifacts { get; init; }
}
