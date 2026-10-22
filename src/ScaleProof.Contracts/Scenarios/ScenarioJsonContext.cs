using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Scenarios;

/// <summary>
/// Serialization metadata for schema-validated scenario v1 input. This does not
/// resolve defaults, authorize execution, or replace JSON Schema validation.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(ScenarioV1))]
[JsonSerializable(typeof(ScenarioSecretReference))]
[JsonSerializable(typeof(ScenarioDatasetPick))]
public partial class ScenarioJsonContext : JsonSerializerContext;

/// <summary>Wire enums are strings; numeric enum values are never accepted.</summary>
public sealed class ScenarioStringEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(allowIntegerValues: false)
    where TEnum : struct, Enum;
