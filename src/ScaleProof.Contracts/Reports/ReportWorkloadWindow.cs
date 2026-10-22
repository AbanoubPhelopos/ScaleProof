using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportWorkloadWindow
{
    [JsonPropertyName("name")]
    public required ReportWindow Name { get; init; }

    [JsonPropertyName("start")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? Start { get; init; }

    [JsonPropertyName("durationMs")]
    public required long DurationMs { get; init; }
}
