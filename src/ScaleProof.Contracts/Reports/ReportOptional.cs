using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaleProof.Contracts.Reports;

/// <summary>Distinguishes an absent member from a present value, including explicit null.</summary>
public readonly record struct ReportOptional<T>
{
    public ReportOptional(T value)
    {
        Value = value;
        IsPresent = true;
    }

    public bool IsPresent { get; }

    public T Value { get; }
}

public sealed class ReportOptionalConverter<T> : JsonConverter<ReportOptional<T>>
{
    public override bool HandleNull => true;

    public override ReportOptional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(JsonSerializer.Deserialize<T>(ref reader, options)!);

    public override void Write(Utf8JsonWriter writer, ReportOptional<T> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.Value, options);
}
