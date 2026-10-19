using System.Xml.Linq;

namespace ScaleProof.UnitTests.Architecture;

internal static class DependencyPolicy
{
    internal static readonly IReadOnlyDictionary<string, string[]> AllowedProjects =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [ProjectNames.Domain] = [],
            [ProjectNames.Contracts] = [],
            [ProjectNames.Application] = [ProjectNames.Domain],
            [ProjectNames.Execution] = [ProjectNames.Contracts],
            [ProjectNames.Infrastructure] = [ProjectNames.Application, ProjectNames.Domain],
            [ProjectNames.Api] = [ProjectNames.Application, ProjectNames.Contracts, ProjectNames.Infrastructure],
            [ProjectNames.Worker] = [ProjectNames.Application, ProjectNames.Contracts, ProjectNames.Infrastructure],
            [ProjectNames.Agent] = [ProjectNames.Contracts, ProjectNames.Execution],
        };

    internal static string[] ProjectViolations(string project, XDocument document)
    {
        var allowed = AllowedProjects[project];
        List<string> violations = [];
        foreach (var reference in document.Descendants(ProjectXml.ProjectReference))
        {
            var path = ((string?)reference.Attribute(ProjectXml.IncludeAttribute) ?? string.Empty).Replace('\\', '/');
            var name = Path.GetFileNameWithoutExtension(path);
            if (!allowed.Any(dependency => name == ProjectNames.FullName(dependency)))
            {
                violations.Add($"{project} -> {name}");
            }
        }

        var isolated = IsIsolated(project);
        if (isolated && (string?)document.Root?.Attribute(ProjectXml.SdkAttribute) != ProjectXml.BaseSdk)
        {
            violations.Add($"{project}: non-base SDK");
        }

        foreach (var reference in document.Descendants().Where(element =>
                     element.Name.LocalName is ProjectXml.PackageReference or ProjectXml.FrameworkReference or ProjectXml.AssemblyReference))
        {
            var name = (string?)reference.Attribute(ProjectXml.IncludeAttribute) ?? string.Empty;
            if (isolated || (ForbidsEntityFramework(project)
                             && name.StartsWith(DependencyNames.EntityFrameworkCore, StringComparison.Ordinal))
                         || (ForbidsAspNetCore(project)
                             && name.StartsWith(DependencyNames.AspNetCore, StringComparison.Ordinal)))
            {
                violations.Add($"{project}: {name}");
            }
        }

        return violations.ToArray();
    }

    internal static bool IsIsolated(string project) => project is ProjectNames.Domain or ProjectNames.Contracts;

    internal static bool ForbidsEntityFramework(string project) =>
        IsIsolated(project) || project is ProjectNames.Application or ProjectNames.Execution or ProjectNames.Agent;

    internal static bool ForbidsAspNetCore(string project) =>
        IsIsolated(project) || project is ProjectNames.Application or ProjectNames.Execution;

    internal static bool IsBaseLibrary(string assembly) =>
        assembly is "System" or "mscorlib" or "netstandard" or "Microsoft.CSharp"
        || assembly.StartsWith("System.", StringComparison.Ordinal);
}
