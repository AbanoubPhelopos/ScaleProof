using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

[JsonConverter(typeof(ReportStringEnumConverter<ReportOutcome>))]
public enum ReportOutcome
{
    [JsonStringEnumMemberName("pass")]
    Pass,

    [JsonStringEnumMemberName("fail")]
    Fail,

    [JsonStringEnumMemberName("warn")]
    Warn,
}
