using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

[JsonConverter(typeof(ScenarioStringEnumConverter<ScenarioEngine>))]
public enum ScenarioEngine
{
    [JsonStringEnumMemberName("k6")]
    K6,
}
