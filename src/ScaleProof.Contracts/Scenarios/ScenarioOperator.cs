using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

[JsonConverter(typeof(ScenarioStringEnumConverter<ScenarioOperator>))]
public enum ScenarioOperator
{
    [JsonStringEnumMemberName("<")]
    LessThan,

    [JsonStringEnumMemberName("<=")]
    LessThanOrEqual,

    [JsonStringEnumMemberName(">")]
    GreaterThan,

    [JsonStringEnumMemberName(">=")]
    GreaterThanOrEqual,
}
