using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Findings;

/// <summary>Finding v1 wire data; schema validation and evaluation are separate boundaries.</summary>
public sealed record FindingEvidence
{
    [JsonPropertyName("kind")]
    public required FindingEvidenceKind Kind { get; init; }

    [JsonPropertyName("ref")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ref { get; init; }

    [JsonPropertyName("request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Request { get; init; }

    [JsonPropertyName("aggregation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Aggregation { get; init; }

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Value { get; init; }

    [JsonPropertyName("unit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Unit { get; init; }

    [JsonPropertyName("sampleCount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? SampleCount { get; init; }

    [JsonPropertyName("count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Count { get; init; }

    [JsonPropertyName("note")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Note { get; init; }
}
