using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportDatasetReference
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("hash")]
    public required string Hash { get; init; }
}
