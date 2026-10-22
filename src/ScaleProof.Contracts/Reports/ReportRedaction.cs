using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportRedaction
{
    [JsonPropertyName("policyVersion")]
    public required string PolicyVersion { get; init; }

    [JsonPropertyName("redactedFields")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? RedactedFields { get; init; }

    [JsonPropertyName("partial")]
    public required bool Partial { get; init; }
}
