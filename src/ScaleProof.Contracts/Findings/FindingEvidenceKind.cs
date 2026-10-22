using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Findings;

[JsonConverter(typeof(FindingStringEnumConverter<FindingEvidenceKind>))]
public enum FindingEvidenceKind
{
    [JsonStringEnumMemberName("metric")]
    Metric,

    [JsonStringEnumMemberName("trace")]
    Trace,

    [JsonStringEnumMemberName("fingerprint")]
    Fingerprint,

    [JsonStringEnumMemberName("artifact")]
    Artifact,

    [JsonStringEnumMemberName("threshold")]
    Threshold,

    [JsonStringEnumMemberName("quality")]
    Quality,

    [JsonStringEnumMemberName("note")]
    Note,
}
