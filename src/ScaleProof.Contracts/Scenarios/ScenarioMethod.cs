using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

[JsonConverter(typeof(ScenarioStringEnumConverter<ScenarioMethod>))]
public enum ScenarioMethod
{
    [JsonStringEnumMemberName("GET")]
    Get,

    [JsonStringEnumMemberName("HEAD")]
    Head,

    [JsonStringEnumMemberName("OPTIONS")]
    Options,

    [JsonStringEnumMemberName("POST")]
    Post,

    [JsonStringEnumMemberName("PUT")]
    Put,

    [JsonStringEnumMemberName("PATCH")]
    Patch,

    [JsonStringEnumMemberName("DELETE")]
    Delete,
}
