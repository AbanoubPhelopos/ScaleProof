using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

[JsonConverter(typeof(ReportStringEnumConverter<ReportQualityStatus>))]
public enum ReportQualityStatus
{
    [JsonStringEnumMemberName("valid")]
    Valid,

    [JsonStringEnumMemberName("inconclusive")]
    Inconclusive,

    [JsonStringEnumMemberName("invalid")]
    Invalid,
}
