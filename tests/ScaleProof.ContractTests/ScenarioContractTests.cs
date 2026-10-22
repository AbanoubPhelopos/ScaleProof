using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using VerifyXunit;
using ScaleProof.Contracts.Scenarios;
using ScaleProof.ContractTests.Infrastructure;

namespace ScaleProof.ContractTests;

public sealed class ScenarioContractTests
{
    private const string Valid = "valid";
    private const string Invalid = "invalid";
    private const string Expectations = "expectations.json";
    private const string Smoke = "smoke.json";
    private const string Fixed = "fixed-vus.json";
    private const string Requests = "requests";
    private const string Thresholds = "thresholds";
    private const string Parameters = "params";
    private const string ParameterId = "id";
    private const string Assertions = "assertions";
    private const string JsonPath = "jsonPath";
    private const string Unexpected = "unexpected";
    private const string Value = "value";
    private const string Aggregation = "aggregation";
    private const string Operator = "operator";
    private const string Unit = "unit";
    private const string PreciseNumber = "12345678901234567890.12345678901234567890";
    private static readonly LocalSchemas Schemas = new();
    private static readonly JsonSerializerOptions ExpectationOptions = new() { PropertyNameCaseInsensitive = true };

    public static IEnumerable<object[]> ValidFixtures =>
        Directory.GetFiles(FixtureDirectory(Valid), "*.json").Order(StringComparer.Ordinal)
            .Select(path => new object[] { Path.GetFileName(path) });

    public static IEnumerable<object[]> InvalidFixtures =>
        JsonSerializer.Deserialize<FixtureExpectation[]>(File.ReadAllText(
            Path.Combine(FixtureDirectory(Invalid), Expectations)), ExpectationOptions)!.Select(expectation => new object[]
            {
                expectation.File, expectation.Pointer, expectation.Keyword, expectation.ReaderRejects,
            });

    [Theory]
    [MemberData(nameof(ValidFixtures))]
    public void ValidFixturesSatisfySchemaAndPreserveEveryValue(string file)
    {
        var input = Load(Valid, file);
        Assert.True(Evaluate(input).IsValid);
        var model = Read(input);
        var output = JsonSerializer.SerializeToElement(model, ScenarioJsonContext.Default.ScenarioV1);
        Assert.True(Evaluate(output).IsValid);
        Assert.True(JsonElement.DeepEquals(input, output), $"Known wire values changed in {file}.");
    }

    [Theory]
    [MemberData(nameof(ValidFixtures))]
    public async Task SerializedWireShapeMatchesReviewedSnapshot(string file)
    {
        var model = Read(Load(Valid, file));
        var output = JsonSerializer.Serialize(model, ScenarioJsonContext.Default.ScenarioV1);
        await Verifier.Verify(output)
            .UseParameters(Path.GetFileNameWithoutExtension(file))
            .UseDirectory(Path.Combine(AppContext.BaseDirectory, "Snapshots"))
            .DisableDiff();
    }

    [Theory]
    [MemberData(nameof(InvalidFixtures))]
    public void InvalidFixturesFailAtDocumentedSchemaAndReaderBoundary(
        string file, string instanceLocation, string keyword, bool readerRejects)
    {
        var input = Load(Invalid, file);
        var result = Evaluate(input);
        Assert.False(result.IsValid);
        Assert.NotNull(result.Details);
        Assert.Contains(result.Details, detail =>
            detail.InstanceLocation.ToString() == instanceLocation && detail.Errors?.ContainsKey(keyword) == true);
        if (readerRejects)
        {
            Assert.Throws<JsonException>(() => Read(input));
        }
        // Reader acceptance is not a promise of validity: schema validation must run first.
    }

    [Fact]
    public void EveryInvalidPayloadHasAnExpectation()
    {
        var payloads = Directory.GetFiles(FixtureDirectory(Invalid), "*.json")
            .Select(Path.GetFileName).Where(file => file != Expectations).Order(StringComparer.Ordinal);
        var documented = InvalidFixtures.Select(row => (string)row[0]).Order(StringComparer.Ordinal);
        Assert.Equal(payloads, documented);
    }

    [Fact]
    public void IntegerOverflowIsAReaderFailureEvenWhenSchemaAllowsTheValue()
    {
        var input = JsonNode.Parse(Load(Valid, Fixed).GetRawText())!;
        input["workload"]!["vus"] = JsonNode.Parse("9223372036854775808");
        var element = JsonSerializer.SerializeToElement(input);
        Assert.True(Evaluate(element).IsValid);
        Assert.Throws<JsonException>(() => Read(element));
    }

    [Fact]
    public void OmittedDefaultsStayAbsentWhileExplicitFalseAndEmptyValuesSurvive()
    {
        var model = Read(Load(Valid, Smoke));
        Assert.Null(model.Classification.Mutation);
        Assert.Null(model.Capture);
        Assert.Null(model.Report);
        Assert.Null(model.Workload.Windows.WarmUp);
        Assert.Null(model.Workload.Windows.RampDown);
        Assert.Null(model.Workload.Timeouts);
        Assert.Null(model.Requests[0].Weight);
        var explicitValues = Read(Load(Valid, "explicit-empty.json"));
        Assert.False(explicitValues.Classification.Mutation);
        Assert.NotNull(explicitValues.Capture);
        Assert.Null(explicitValues.Capture.Mode);
        Assert.NotNull(explicitValues.Report);
        Assert.NotNull(explicitValues.Report.Formats);
        Assert.Empty(explicitValues.Report.Formats);
    }

    [Fact]
    public void SecretAndDatasetReferencesRemainTypedAndUnresolved()
    {
        var model = Read(Load(Valid, Fixed));
        var request = model.Requests[0];
        Assert.NotNull(request.Headers);
        var secret = Assert.IsType<ScenarioHeaderValue.Secret>(request.Headers["Authorization"]);
        Assert.Equal("sample-token", secret.Reference.SecretRef);
        Assert.Equal("Bearer {value}", secret.Reference.Format);
        Assert.IsType<ScenarioHeaderValue.Literal>(request.Headers["Accept"]);
        Assert.NotNull(request.Params);
        var dataset = Assert.IsType<ScenarioParameterValue.Dataset>(request.Params[ParameterId]);
        Assert.Equal("sample-ids", dataset.Selection.Dataset);
        Assert.Null(dataset.Selection.Pick);
        Assert.IsType<ScenarioParameterValue.Literal>(request.Params["active"]);
    }

    [Fact]
    public void ArbitraryJsonPreservesExplicitNullVersusOmission()
    {
        var model = Read(Load(Valid, "reserved-shapes.json"));
        Assert.Equal(JsonValueKind.Object, model.Requests[1].Body!.Json.ValueKind);
        Assert.Equal(JsonValueKind.Undefined, model.Requests[2].Body!.Json.ValueKind);
        Assert.Equal(JsonValueKind.Null, model.Requests[3].Body!.Json.ValueKind);
        var fixedModel = Read(Load(Valid, Fixed));
        Assert.Equal(JsonValueKind.Null, fixedModel.Requests[0].Assertions![2].JsonPath!.ExpectedValue.ValueKind);
    }

    [Theory]
    [InlineData(Aggregation)]
    [InlineData(Operator)]
    [InlineData(Unit)]
    public void EveryThresholdEnumValueUsesItsExactSchemaSpelling(string field)
    {
        var values = field switch
        {
            Aggregation => Enum.GetValues<ScenarioAggregation>().Select(value =>
                JsonSerializer.SerializeToElement(value, ScenarioJsonContext.Default.Options)),
            Operator => Enum.GetValues<ScenarioOperator>().Select(value =>
                JsonSerializer.SerializeToElement(value, ScenarioJsonContext.Default.Options)),
            _ => Enum.GetValues<ScenarioUnit>().Select(value =>
                JsonSerializer.SerializeToElement(value, ScenarioJsonContext.Default.Options)),
        };
        foreach (var value in values)
        {
            var input = JsonNode.Parse(Load(Valid, Fixed).GetRawText())!;
            input[Thresholds]![0]![field] = JsonNode.Parse(value.GetRawText());
            var element = JsonSerializer.SerializeToElement(input);
            Assert.True(Evaluate(element).IsValid);
            Assert.True(JsonElement.DeepEquals(element,
                JsonSerializer.SerializeToElement(Read(element), ScenarioJsonContext.Default.ScenarioV1)));
        }
    }

    [Fact]
    public void ParameterNumbersRetainPrecisionBeyondDouble()
    {
        var input = JsonNode.Parse(Load(Valid, Smoke).GetRawText())!;
        input[Requests]![0]![Parameters] = JsonNode.Parse("{\"value\":" + PreciseNumber + "}");
        var output = JsonSerializer.SerializeToElement(Read(JsonSerializer.SerializeToElement(input)),
            ScenarioJsonContext.Default.ScenarioV1);
        Assert.Equal(PreciseNumber, output.GetProperty(Requests)[0].GetProperty(Parameters).GetProperty(Value).GetRawText());
    }

    [Fact]
    public void ThresholdNumbersRetainPrecisionBeyondDouble()
    {
        var input = JsonNode.Parse(Load(Valid, Fixed).GetRawText())!;
        input[Thresholds]![0]![Value] = JsonNode.Parse(PreciseNumber);
        var output = JsonSerializer.SerializeToElement(Read(JsonSerializer.SerializeToElement(input)),
            ScenarioJsonContext.Default.ScenarioV1);
        Assert.True(Evaluate(output).IsValid);
        Assert.Equal(PreciseNumber, output.GetProperty(Thresholds)[0].GetProperty(Value).GetRawText());
    }

    [Fact]
    public void ReaderAlsoRejectsUnknownFieldsInsideAssertionAndDatasetObjects()
    {
        var input = JsonNode.Parse(Load(Valid, Fixed).GetRawText())!;
        input[Requests]![0]![Assertions]![2]![JsonPath]![Unexpected] = true;
        Assert.Throws<JsonException>(() => Read(JsonSerializer.SerializeToElement(input)));
        input = JsonNode.Parse(Load(Valid, Fixed).GetRawText())!;
        input[Requests]![0]![Parameters]![ParameterId]![Unexpected] = true;
        Assert.Throws<JsonException>(() => Read(JsonSerializer.SerializeToElement(input)));
    }

    private static ScenarioV1 Read(JsonElement input) =>
        JsonSerializer.Deserialize(input, ScenarioJsonContext.Default.ScenarioV1)!;

    private static EvaluationResults Evaluate(JsonElement input) => Schemas.Evaluate(ContractNames.Scenario, input);

    private static string FixtureDirectory(string category) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Scenario", "v1", category);

    private static JsonElement Load(string category, string file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixtureDirectory(category), file)));
        return document.RootElement.Clone();
    }

    private sealed record FixtureExpectation(string File, string Pointer, string Keyword, bool ReaderRejects);
}
