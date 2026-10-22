using System.Reflection;
using System.Xml.Linq;
using NetArchTest.Rules;

namespace ScaleProof.UnitTests.Architecture;

public sealed class DependencyTests
{
    public static TheoryData<string> Projects => new(DependencyPolicy.AllowedProjects.Keys);

    [Fact]
    public void ProjectInventoryContainsExactlyTheEightProductionProjects()
    {
        var files = Directory.GetFiles(ProjectDirectory, "*" + ProjectNames.ProjectExtension)
            .Select(Path.GetFileNameWithoutExtension).Order().ToArray();
        var expected = DependencyPolicy.AllowedProjects.Keys.Select(ProjectNames.FullName).Order();
        Assert.Equal(expected, files);
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void ProjectReferencesRespectTheAllowedGraph(string project)
    {
        var document = XDocument.Load(Path.Combine(ProjectDirectory, ProjectNames.FileName(project)));
        Assert.Empty(DependencyPolicy.ProjectViolations(project, document));
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void CompiledAssemblyReferencesRespectBoundaries(string project)
    {
        var assembly = LoadProductionAssembly(project);
        var allowed = DependencyPolicy.AllowedProjects[project].Select(ProjectNames.AssemblyName).ToArray();
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            var name = Assert.IsType<string>(reference.Name);
            if (DependencyPolicy.IsIsolated(project))
            {
                Assert.True(DependencyPolicy.IsBaseLibrary(name), $"{project} references {name}");
            }
            else if (name.StartsWith(ProjectNames.NamespacePrefix, StringComparison.Ordinal) || name == ProjectNames.AgentAssembly)
            {
                Assert.Contains(name, allowed);
            }

            if (DependencyPolicy.ForbidsEntityFramework(project))
            {
                Assert.False(name.StartsWith(DependencyNames.EntityFrameworkCore, StringComparison.Ordinal));
            }
        }
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void ProductionTypesDoNotDependOnForbiddenNamespaces(string project)
    {
        var assembly = LoadProductionAssembly(project);
        var forbidden = DependencyPolicy.AllowedProjects.Keys
            .Except(DependencyPolicy.AllowedProjects[project].Append(project))
            .Select(ProjectNames.FullName).ToList();
        if (DependencyPolicy.ForbidsEntityFramework(project))
        {
            forbidden.Add(DependencyNames.EntityFrameworkCore);
        }

        if (DependencyPolicy.ForbidsAspNetCore(project))
        {
            forbidden.Add(DependencyNames.AspNetCore);
        }

        if (DependencyPolicy.IsIsolated(project))
        {
            forbidden.Add(DependencyNames.Extensions);
        }

        if (project == ProjectNames.Application)
        {
            forbidden.Add(DependencyNames.Http);
        }

        foreach (var dependency in forbidden)
        {
            var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOn(dependency).GetResult();
            Assert.True(result.IsSuccessful, $"{project} has a dependency on {dependency}");
        }
    }

    [Theory]
    [InlineData(ProjectNames.Agent, ProjectNames.Api)]
    [InlineData(ProjectNames.Agent, ProjectNames.Application)]
    [InlineData(ProjectNames.Agent, ProjectNames.Infrastructure)]
    [InlineData(ProjectNames.Domain, ProjectNames.Contracts)]
    [InlineData(ProjectNames.Contracts, ProjectNames.Domain)]
    [InlineData(ProjectNames.Worker, ProjectNames.Api)]
    [InlineData(ProjectNames.Worker, ProjectNames.Execution)]
    public void ForbiddenProjectReferenceIsDetectedEvenWithoutCompiledTypes(string project, string dependency)
    {
        var referencePath = Path.Combine("..", ProjectNames.FullName(dependency), ProjectNames.FileName(dependency));
        var document = ProjectXml.WithReference(ProjectXml.ProjectReference, referencePath);
        Assert.Contains($"{project} -> {ProjectNames.FullName(dependency)}", DependencyPolicy.ProjectViolations(project, document));
    }

    [Theory]
    [InlineData(ProjectNames.Domain, ProjectXml.PackageReference, "Example.Library")]
    [InlineData(ProjectNames.Contracts, ProjectXml.FrameworkReference, DependencyNames.AspNetCore + ".App")]
    [InlineData(ProjectNames.Agent, ProjectXml.PackageReference, DependencyNames.EntityFrameworkCore)]
    [InlineData(ProjectNames.Application, ProjectXml.PackageReference, DependencyNames.EntityFrameworkCore + ".Relational")]
    public void ForbiddenNonProjectDependencyIsDetected(string project, string kind, string dependency)
    {
        var document = ProjectXml.WithReference(kind, dependency);
        Assert.Contains($"{project}: {dependency}", DependencyPolicy.ProjectViolations(project, document));
    }

    [Fact]
    public void NetArchTestDetectsARealCompiledNamespaceViolation()
    {
        var result = Types.InAssembly(typeof(Controls.Violating.Consumer).Assembly)
            .That().ResideInNamespace(typeof(Controls.Violating.Consumer).Namespace!)
            .ShouldNot().HaveDependencyOn(DependencyNames.Http).GetResult();
        Assert.False(result.IsSuccessful);
        Assert.Contains(typeof(Controls.Violating.Consumer), result.FailingTypes);
    }

    [Fact]
    public void NetArchTestAcceptsTheAllowedControl()
    {
        var result = Types.InAssembly(typeof(Controls.Allowed.Consumer).Assembly)
            .That().ResideInNamespace(typeof(Controls.Allowed.Consumer).Namespace!)
            .ShouldNot().HaveDependencyOn(DependencyNames.Http).GetResult();
        Assert.True(result.IsSuccessful);
    }

    private static string ProjectDirectory => Path.Combine(AppContext.BaseDirectory, "Architecture", "Projects");

    private static Assembly LoadProductionAssembly(string project) =>
        Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, $"{ProjectNames.AssemblyName(project)}.dll"));
}
