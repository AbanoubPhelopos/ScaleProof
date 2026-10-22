using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Findings;

[JsonConverter(typeof(FindingStringEnumConverter<FindingSeverity>))]
public enum FindingSeverity
{
    [JsonStringEnumMemberName("info")]
    Info,

    [JsonStringEnumMemberName("low")]
    Low,

    [JsonStringEnumMemberName("medium")]
    Medium,

    [JsonStringEnumMemberName("high")]
    High,

    [JsonStringEnumMemberName("critical")]
    Critical,
}
