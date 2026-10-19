using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using ScaleProof.ContractTests.Infrastructure;
using VerifyXunit;

namespace ScaleProof.ContractTests;

public sealed class SchemaInfrastructureTests
{
    private static readonly JsonElement EmptyObject = JsonSerializer.SerializeToElement(new { });

    [Theory]
    [InlineData(ContractNames.Scenario)]
    [InlineData(ContractNames.Finding)]
    [InlineData(ContractNames.Report)]
    public void ExistingSchemasBuildAndRejectMissingRequiredFields(string contract)
    {
        var schemas = new LocalSchemas();
        var result = schemas.Evaluate(contract, EmptyObject);
        AssertInvalidAt(result, string.Empty, JsonFields.Required);
    }

    [Fact]
    public void ReportFindingReferenceResolvesToTheCheckedInSchema()
    {
        var schemas = new LocalSchemas();
        using var report = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.True(schemas.Evaluate(ContractNames.Report, report.RootElement).IsValid);

        var invalid = JsonNode.Parse(report.RootElement.GetRawText())!;
        invalid[JsonFields.Findings]![0]![JsonFields.Status] = "unknown_status";
        var result = schemas.Evaluate(ContractNames.Report, JsonSerializer.SerializeToElement(invalid));
        AssertInvalidAt(result, JsonFields.FirstFindingStatusPointer, JsonFields.Enum);
    }

    [Fact]
    public void ReportDateTimeFormatsAreAsserted()
    {
        var schemas = new LocalSchemas();
        var report = JsonNode.Parse(File.ReadAllText(ReportPath))!;
        report[JsonFields.GeneratedAt] = "invalid-date";
        var result = schemas.Evaluate(ContractNames.Report, JsonSerializer.SerializeToElement(report));
        AssertInvalidAt(result, JsonFields.GeneratedAtPointer, JsonFields.Format);
    }

    [Theory]
    [InlineData("https://unregistered.invalid/missing.schema.json")]
    [InlineData("file:///unregistered/missing.schema.json")]
    public void UnregisteredReferencesFailWithoutFetching(string uri)
    {
        var schemas = new LocalSchemas();
        var error = Assert.Throws<InvalidOperationException>(() =>
        {
            var schema = schemas.Build(new JsonObject { [JsonFields.Reference] = uri }.ToJsonString());
            schema.Evaluate(EmptyObject, LocalSchemas.EvaluationSettings());
        });
        Assert.Contains(LocalSchemas.UnregisteredSchemaError, error.Message, StringComparison.Ordinal);
        Assert.Contains(uri, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SchemaIdentityAndRequiredFieldInventoryMatchesReviewedSnapshot()
    {
        var inventory = ContractNames.BuildOrder.Order(StringComparer.Ordinal).Select(contract =>
        {
            using var document = JsonDocument.Parse(File.ReadAllText(LocalSchemas.SchemaPath(contract)));
            var root = document.RootElement;
            return new
            {
                Id = root.GetProperty(JsonFields.Id).GetString(),
                Required = root.GetProperty(JsonFields.Required).EnumerateArray().Select(value => value.GetString()).Order().ToArray(),
                AllowsAdditionalProperties = !root.TryGetProperty(JsonFields.AdditionalProperties, out var additional) || additional.ValueKind != JsonValueKind.False,
            };
        }).ToArray();

        await Verifier.Verify(inventory)
            .UseDirectory(Path.Combine(AppContext.BaseDirectory, "Snapshots"))
            .DisableDiff();
    }

    private static string ReportPath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Foundation", "v1", "report-with-finding.json");

    private static void AssertInvalidAt(EvaluationResults result, string pointer, string keyword)
    {
        Assert.False(result.IsValid);
        Assert.NotNull(result.Details);
        Assert.Contains(result.Details, detail =>
            detail.InstanceLocation.ToString() == pointer && detail.Errors?.ContainsKey(keyword) == true);
    }
}
