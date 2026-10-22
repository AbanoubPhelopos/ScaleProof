using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportArtifact
{
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("bytes")]
    public required long Bytes { get; init; }

    [JsonPropertyName("mediaType")]
    public required string MediaType { get; init; }
}
