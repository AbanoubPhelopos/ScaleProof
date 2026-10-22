using System.Xml.Linq;

namespace ScaleProof.UnitTests.Architecture;

internal static class ProjectXml
{
    internal const string ProjectReference = "ProjectReference";
    internal const string PackageReference = "PackageReference";
    internal const string FrameworkReference = "FrameworkReference";
    internal const string AssemblyReference = "Reference";
    internal const string IncludeAttribute = "Include";
    internal const string SdkAttribute = "Sdk";
    internal const string BaseSdk = "Microsoft.NET.Sdk";

    internal static XDocument WithReference(string kind, string dependency) => new(
        new XElement("Project", new XAttribute(SdkAttribute, BaseSdk),
            new XElement("ItemGroup", new XElement(kind, new XAttribute(IncludeAttribute, dependency)))));
}
