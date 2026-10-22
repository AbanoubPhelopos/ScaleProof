using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

[JsonConverter(typeof(ReportStringEnumConverter<ReportThresholdStatus>))]
public enum ReportThresholdStatus
{
    [JsonStringEnumMemberName("pass")]
    Pass,

    [JsonStringEnumMemberName("fail")]
    Fail,

    [JsonStringEnumMemberName("inconclusive")]
    Inconclusive,
}
