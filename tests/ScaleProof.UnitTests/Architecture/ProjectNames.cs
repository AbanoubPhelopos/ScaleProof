namespace ScaleProof.UnitTests.Architecture;

internal static class ProjectNames
{
    internal const string Domain = "Domain";
    internal const string Contracts = "Contracts";
    internal const string Application = "Application";
    internal const string Execution = "Execution";
    internal const string Infrastructure = "Infrastructure";
    internal const string Api = "Api";
    internal const string Worker = "Worker";
    internal const string Agent = "Agent";
    internal const string NamespacePrefix = "ScaleProof.";
    internal const string AgentAssembly = "scaleproof";
    internal const string ProjectExtension = ".csproj";

    internal static string FullName(string project) => NamespacePrefix + project;
    internal static string FileName(string project) => FullName(project) + ProjectExtension;
    internal static string AssemblyName(string project) => project == Agent ? AgentAssembly : FullName(project);
}
