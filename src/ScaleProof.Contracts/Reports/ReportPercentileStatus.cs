using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

[JsonConverter(typeof(ReportStringEnumConverter<ReportPercentileStatus>))]
public enum ReportPercentileStatus
{
    [JsonStringEnumMemberName("stable")]
    Stable,

    [JsonStringEnumMemberName("unstable")]
    Unstable,

    [JsonStringEnumMemberName("suppressed")]
    Suppressed,
}
