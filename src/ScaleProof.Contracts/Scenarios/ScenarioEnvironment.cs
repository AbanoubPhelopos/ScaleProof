using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

[JsonConverter(typeof(ScenarioStringEnumConverter<ScenarioEnvironment>))]
public enum ScenarioEnvironment
{
    [JsonStringEnumMemberName("local")]
    Local,

    [JsonStringEnumMemberName("test")]
    Test,

    [JsonStringEnumMemberName("staging")]
    Staging,
}
