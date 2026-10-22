using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

[JsonConverter(typeof(ScenarioStringEnumConverter<ScenarioAggregation>))]
public enum ScenarioAggregation
{
    [JsonStringEnumMemberName("count")]
    Count,

    [JsonStringEnumMemberName("rate")]
    Rate,

    [JsonStringEnumMemberName("value")]
    Value,

    [JsonStringEnumMemberName("min")]
    Min,

    [JsonStringEnumMemberName("max")]
    Max,

    [JsonStringEnumMemberName("mean")]
    Mean,

    [JsonStringEnumMemberName("p50")]
    P50,

    [JsonStringEnumMemberName("p90")]
    P90,

    [JsonStringEnumMemberName("p95")]
    P95,

    [JsonStringEnumMemberName("p99")]
    P99,

    [JsonStringEnumMemberName("p99.9")]
    P99Point9,

    [JsonStringEnumMemberName("ratio_of_offered")]
    RatioOfOffered,
}
