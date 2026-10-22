using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportIdentity
{
    [JsonPropertyName("scenario")]
    public required ReportVersionedReference Scenario { get; init; }

    [JsonPropertyName("target")]
    public required ReportVersionedReference Target { get; init; }

    [JsonPropertyName("policy")]
    public required ReportPolicyReference Policy { get; init; }

    [JsonPropertyName("datasets")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReportDatasetReference[]? Datasets { get; init; }

    [JsonPropertyName("engine")]
    public required ReportEngine Engine { get; init; }

    [JsonPropertyName("build")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Build { get; init; }

    [JsonPropertyName("environment")]
    public required ReportEnvironment Environment { get; init; }
}
