using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Findings;

/// <summary>
/// Metadata for schema-validated finding v1 data. Additive fields are ignored;
/// this reader neither evaluates checks nor enforces the evidence requirement.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(FindingV1))]
public partial class FindingJsonContext : JsonSerializerContext;

/// <summary>Finding wire enums accept string values only.</summary>
public sealed class FindingStringEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(allowIntegerValues: false)
    where TEnum : struct, Enum;
