using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Findings;

[JsonConverter(typeof(FindingStringEnumConverter<FindingStatus>))]
public enum FindingStatus
{
    [JsonStringEnumMemberName("pass")]
    Pass,

    [JsonStringEnumMemberName("fail")]
    Fail,

    [JsonStringEnumMemberName("warn")]
    Warn,

    [JsonStringEnumMemberName("inconclusive")]
    Inconclusive,

    [JsonStringEnumMemberName("not_applicable")]
    NotApplicable,

    [JsonStringEnumMemberName("skipped")]
    Skipped,

    [JsonStringEnumMemberName("error")]
    Error,
}
