using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Generated v1 metadata; additive properties are ignored at every typed level.</summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    RespectNullableAnnotations = true,
    Converters = new[] { typeof(ReportUtcInstantConverter) })]
[JsonSerializable(typeof(ReportV1))]
[JsonSerializable(typeof(DateTimeOffset?))]
[JsonSerializable(typeof(long?))]
[JsonSerializable(typeof(ReportPercentileStatus?))]
[JsonSerializable(typeof(ReportUnavailableReason?))]
public partial class ReportJsonContext : JsonSerializerContext;

public sealed class ReportStringEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(allowIntegerValues: false)
    where TEnum : struct, Enum;
