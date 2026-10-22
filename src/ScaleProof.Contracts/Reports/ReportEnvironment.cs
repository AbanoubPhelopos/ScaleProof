using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportEnvironment
{
    [JsonPropertyName("fingerprint")]
    public required string Fingerprint { get; init; }

    [JsonPropertyName("agentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AgentId { get; init; }
}
