using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

[JsonConverter(typeof(ScenarioStringEnumConverter<ScenarioCaptureMode>))]
public enum ScenarioCaptureMode
{
    [JsonStringEnumMemberName("metadata_only")]
    MetadataOnly,

    [JsonStringEnumMemberName("allow_listed")]
    AllowListed,
}
