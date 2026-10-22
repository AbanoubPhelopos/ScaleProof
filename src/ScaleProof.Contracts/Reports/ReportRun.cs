using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportRun
{
    [JsonPropertyName("runId")]
    public required string RunId { get; init; }

    [JsonPropertyName("attemptId")]
    public required string AttemptId { get; init; }

    [JsonPropertyName("attemptOrdinal")]
    public required long AttemptOrdinal { get; init; }

    [JsonPropertyName("projectId")]
    public required string ProjectId { get; init; }

    [JsonPropertyName("lifecycle")]
    public required ReportLifecycle Lifecycle { get; init; }

    [JsonPropertyName("qualityStatus")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ReportQualityStatus? QualityStatus { get; init; }

    [JsonPropertyName("outcome")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required ReportOutcome? Outcome { get; init; }

    [JsonPropertyName("terminationReason")]
    public required string TerminationReason { get; init; }

    [JsonPropertyName("executionLocation")]
    public required ReportExecutionLocation ExecutionLocation { get; init; }

    [JsonPropertyName("submittedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? SubmittedAt { get; init; }

    [JsonPropertyName("startedAt")]
    [JsonConverter(typeof(ReportOptionalConverter<DateTimeOffset?>))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ReportOptional<DateTimeOffset?> StartedAt { get; init; }

    [JsonPropertyName("finishedAt")]
    [JsonConverter(typeof(ReportOptionalConverter<DateTimeOffset?>))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ReportOptional<DateTimeOffset?> FinishedAt { get; init; }
}
