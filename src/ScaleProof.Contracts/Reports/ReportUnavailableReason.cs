using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

[JsonConverter(typeof(ReportStringEnumConverter<ReportUnavailableReason>))]
public enum ReportUnavailableReason
{
    [JsonStringEnumMemberName("insufficient_samples")]
    InsufficientSamples,

    [JsonStringEnumMemberName("not_collected")]
    NotCollected,

    [JsonStringEnumMemberName("telemetry_unavailable")]
    TelemetryUnavailable,

    [JsonStringEnumMemberName("redacted")]
    Redacted,

    [JsonStringEnumMemberName("engine_unsupported")]
    EngineUnsupported,
}
