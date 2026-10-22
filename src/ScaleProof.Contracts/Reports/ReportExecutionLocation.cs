using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

[JsonConverter(typeof(ReportStringEnumConverter<ReportExecutionLocation>))]
public enum ReportExecutionLocation
{
    [JsonStringEnumMemberName("server_worker")]
    ServerWorker,

    [JsonStringEnumMemberName("local_agent")]
    LocalAgent,

    [JsonStringEnumMemberName("diagnostic_tunnel")]
    DiagnosticTunnel,
}
