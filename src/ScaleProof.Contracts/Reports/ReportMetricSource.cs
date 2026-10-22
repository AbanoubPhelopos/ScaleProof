using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

[JsonConverter(typeof(ReportStringEnumConverter<ReportMetricSource>))]
public enum ReportMetricSource
{
    [JsonStringEnumMemberName("k6")]
    K6,

    [JsonStringEnumMemberName("adapter")]
    Adapter,

    [JsonStringEnumMemberName("worker")]
    Worker,

    [JsonStringEnumMemberName("agent")]
    Agent,

    [JsonStringEnumMemberName("otel")]
    Otel,
}
