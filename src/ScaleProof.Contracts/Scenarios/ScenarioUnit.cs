using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

[JsonConverter(typeof(ScenarioStringEnumConverter<ScenarioUnit>))]
public enum ScenarioUnit
{
    [JsonStringEnumMemberName("ms")]
    Milliseconds,

    [JsonStringEnumMemberName("s")]
    Seconds,

    [JsonStringEnumMemberName("count")]
    Count,

    [JsonStringEnumMemberName("ratio")]
    Ratio,

    [JsonStringEnumMemberName("iter/s")]
    IterationsPerSecond,

    [JsonStringEnumMemberName("req/s")]
    RequestsPerSecond,

    [JsonStringEnumMemberName("bytes")]
    Bytes,
}
