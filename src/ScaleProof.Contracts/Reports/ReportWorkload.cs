using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Report v1 wire data; no measurements or verdicts are computed.</summary>
public sealed record ReportWorkload
{
    [JsonPropertyName("profile")]
    public required ReportProfile Profile { get; init; }

    [JsonPropertyName("windows")]
    public required ReportWorkloadWindow[] Windows { get; init; }

    [JsonPropertyName("offeredRate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement OfferedRate { get; init; }

    [JsonPropertyName("achievedRate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement AchievedRate { get; init; }

    [JsonPropertyName("droppedIterations")]
    [JsonConverter(typeof(ReportOptionalConverter<long?>))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ReportOptional<long?> DroppedIterations { get; init; }
}
