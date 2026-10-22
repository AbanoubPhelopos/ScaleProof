using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Findings;

/// <summary>Finding v1 wire data; schema validation and evaluation are separate boundaries.</summary>
public sealed record FindingV1
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("checkId")]
    public required string CheckId { get; init; }

    [JsonPropertyName("checkVersion")]
    public required long CheckVersion { get; init; }

    [JsonPropertyName("status")]
    public required FindingStatus Status { get; init; }

    [JsonPropertyName("severity")]
    public required FindingSeverity Severity { get; init; }

    [JsonPropertyName("confidence")]
    public required FindingConfidence Confidence { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("evidence")]
    public required FindingEvidence[] Evidence { get; init; }

    [JsonPropertyName("limitations")]
    public required string[] Limitations { get; init; }

    [JsonPropertyName("recommendations")]
    public required string[] Recommendations { get; init; }

    [JsonPropertyName("docs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Docs { get; init; }
}
