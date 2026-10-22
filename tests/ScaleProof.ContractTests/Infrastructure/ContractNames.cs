namespace ScaleProof.ContractTests.Infrastructure;

internal static class ContractNames
{
    internal const string Scenario = "scenario";
    internal const string Finding = "check-result";
    internal const string Report = "report";

    // References must be registered before their consumers.
    internal static readonly IReadOnlyList<string> BuildOrder = Array.AsReadOnly<string>([Finding, Scenario, Report]);
}
