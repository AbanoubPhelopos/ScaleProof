using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

[JsonConverter(typeof(ScenarioStringEnumConverter<ScenarioProfile>))]
public enum ScenarioProfile
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
