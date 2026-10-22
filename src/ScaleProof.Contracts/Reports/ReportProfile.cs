using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

[JsonConverter(typeof(ReportStringEnumConverter<ReportProfile>))]
public enum ReportProfile
{
    [JsonStringEnumMemberName("smoke")]
    Smoke,

    [JsonStringEnumMemberName("fixed_vus")]
    FixedVus,

    [JsonStringEnumMemberName("arrival_rate")]
    ArrivalRate,

    [JsonStringEnumMemberName("steps")]
    Steps,
}
