using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Findings;

[JsonConverter(typeof(FindingStringEnumConverter<FindingConfidence>))]
public enum FindingConfidence
{
    [JsonStringEnumMemberName("low")]
    Low,

    [JsonStringEnumMemberName("medium")]
    Medium,

    [JsonStringEnumMemberName("high")]
    High,
}
