using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

[JsonConverter(typeof(ScenarioStringEnumConverter<ScenarioDatasetSelection>))]
public enum ScenarioDatasetSelection
{
    [JsonStringEnumMemberName("round_robin")]
    RoundRobin,

    [JsonStringEnumMemberName("random_seeded")]
    RandomSeeded,
}
