using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

[JsonConverter(typeof(ScenarioStringEnumConverter<ScenarioReportFormat>))]
public enum ScenarioReportFormat
{
    [JsonStringEnumMemberName("json")]
    Json,

    [JsonStringEnumMemberName("markdown")]
    Markdown,
}
