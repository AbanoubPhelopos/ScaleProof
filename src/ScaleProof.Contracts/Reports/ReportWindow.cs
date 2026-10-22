using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

[JsonConverter(typeof(ReportStringEnumConverter<ReportWindow>))]
public enum ReportWindow
{
    [JsonStringEnumMemberName("warmup")]
    Warmup,

    [JsonStringEnumMemberName("measurement")]
    Measurement,

    [JsonStringEnumMemberName("rampdown")]
    Rampdown,
}
