using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportQuality
{
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ReportQualityStatus? Status { get; init; }

    [JsonPropertyName("policyVersion")]
    public required string PolicyVersion { get; init; }

    [JsonPropertyName("reasons")]
    public required JsonElement[] Reasons { get; init; }
}
