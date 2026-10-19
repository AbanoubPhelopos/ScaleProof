namespace ScaleProof.ContractTests.Infrastructure;

internal static class JsonFields
{
    private const string PointerSeparator = "/";

    internal const string Id = "$id";
    internal const string Reference = "$ref";
    internal const string Required = "required";
    internal const string Enum = "enum";
    internal const string Format = "format";
    internal const string AdditionalProperties = "additionalProperties";
    internal const string Findings = "findings";
    internal const string Status = "status";
    internal const string GeneratedAt = "generatedAt";
    internal const string FirstFindingStatusPointer = PointerSeparator + Findings + PointerSeparator + "0" + PointerSeparator + Status;
    internal const string GeneratedAtPointer = PointerSeparator + GeneratedAt;
}
