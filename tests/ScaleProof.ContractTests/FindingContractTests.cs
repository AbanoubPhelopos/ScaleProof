using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using ScaleProof.Contracts.Findings;
using ScaleProof.ContractTests.Infrastructure;
using VerifyXunit;

namespace ScaleProof.ContractTests;

public sealed class FindingContractTests
{
    private const string Valid = "valid";
    private const string Invalid = "invalid";
    private const string Expectations = "expectations.json";
    private const string FailEvidence = "fail-evidence.json";
    private const string WarnEvidence = "warn-evidence.json";
    private const string Evidence = "evidence";
    private const string CheckVersion = "checkVersion";
    private const string Count = "count";
    private const string Value = "value";
    private const string Required = "required";
    private const string FutureField = "futureField";
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

    public static IEnumerable<object[]> RequiredFields
    {
        get
        {
            using var document = JsonDocument.Parse(File.ReadAllText(LocalSchemas.SchemaPath(ContractNames.Finding)));
            return document.RootElement.GetProperty(Required).EnumerateArray()
                .Select(member => new object[] { member.GetString()! }).ToArray();
        }
    }

    public static IEnumerable<object[]> SeverityConfidencePairs =>
        from severity in Enum.GetValues<FindingSeverity>()
        from confidence in Enum.GetValues<FindingConfidence>()
        select new object[] { severity, confidence };

    [Theory]
    [MemberData(nameof(ValidFixtures))]
    public void ValidFixturesSatisfySchemaAndPreserveKnownValues(string file)
    {
        var input = Load(Valid, file);
        Assert.True(Evaluate(input).IsValid);
        var output = Write(Read(input));
        Assert.True(Evaluate(output).IsValid);
        Assert.True(JsonElement.DeepEquals(input, output), $"Known wire values changed in {file}.");
    }

    [Theory]
    [MemberData(nameof(ValidFixtures))]
    public async Task SerializedWireShapeMatchesReviewedSnapshot(string file)
    {
        var output = JsonSerializer.Serialize(Read(Load(Valid, file)), FindingJsonContext.Default.FindingV1);
        await Verifier.Verify(output)
            .UseParameters(Path.GetFileNameWithoutExtension(file))
            .UseDirectory(Path.Combine(AppContext.BaseDirectory, "Snapshots"))
            .DisableDiff();
    }

    [Theory]
    [MemberData(nameof(InvalidFixtures))]
    public void InvalidFixturesFailAtDocumentedBoundary(string file, string instanceLocation, string keyword, bool readerRejects)
    {
        var input = Load(Invalid, file);
        AssertInvalidAt(Evaluate(input), instanceLocation, keyword);
        if (readerRejects)
        {
            Assert.Throws<JsonException>(() => Read(input));
        }
    }

    [Theory]
    [MemberData(nameof(RequiredFields))]
    public void EveryRequiredMemberIsRequiredBySchemaAndReader(string field)
    {
        var input = JsonNode.Parse(Load(Valid, FailEvidence).GetRawText())!.AsObject();
        Assert.True(input.Remove(field));
        var element = JsonSerializer.SerializeToElement(input);
        AssertInvalidAt(Evaluate(element), string.Empty, Required);
        Assert.Throws<JsonException>(() => Read(element));
    }

    [Theory]
    [MemberData(nameof(SeverityConfidencePairs))]
    public void SeverityAndConfidenceAreIndependent(FindingSeverity severity, FindingConfidence confidence)
    {
        var model = Read(Load(Valid, FailEvidence)) with { Severity = severity, Confidence = confidence };
        var output = Write(model);
        Assert.True(Evaluate(output).IsValid);
        var restored = Read(output);
        Assert.Equal(severity, restored.Severity);
        Assert.Equal(confidence, restored.Confidence);
    }

    [Theory]
    [InlineData(FindingStatus.Fail)]
    [InlineData(FindingStatus.Warn)]
    [InlineData(FindingStatus.Inconclusive)]
    [InlineData(FindingStatus.Error)]
    public void NonExemptStatusesRequireEvidenceOrLimitationAtSchemaBoundary(FindingStatus status)
    {
        var model = Read(Load(Valid, FailEvidence)) with { Status = status, Limitations = [] };
        Assert.True(Evaluate(Write(model)).IsValid);
        var unsupported = model with { Evidence = [] };
        var unsupportedJson = Write(unsupported);
        Assert.False(Evaluate(unsupportedJson).IsValid);
        // DTO reading alone does not enforce schema semantics or turn missing evidence into pass.
        Assert.Equal(status, Read(unsupportedJson).Status);
        var explained = unsupported with { Limitations = ["Required evidence was not collected."] };
        Assert.True(Evaluate(Write(explained)).IsValid);
    }

    [Theory]
    [InlineData(FindingStatus.Pass)]
    [InlineData(FindingStatus.NotApplicable)]
    [InlineData(FindingStatus.Skipped)]
    public void ExemptStatusesMayHaveEmptyEvidenceAndLimitations(FindingStatus status)
    {
        var model = Read(Load(Valid, FailEvidence)) with { Status = status, Evidence = [], Limitations = [] };
        Assert.True(Evaluate(Write(model)).IsValid);
        Assert.Equal(status, Read(Write(model)).Status);
    }

    [Fact]
    public void MissingTelemetryStaysDistinctFromExplicitInapplicability()
    {
        var missing = Read(Load(Valid, "inconclusive-limitations.json"));
        var inapplicable = Read(Load(Valid, "not_applicable.json"));
        Assert.Equal(FindingStatus.Inconclusive, missing.Status);
        Assert.Empty(missing.Evidence);
        Assert.NotEmpty(missing.Limitations);
        Assert.Equal(FindingStatus.NotApplicable, inapplicable.Status);
        Assert.Empty(inapplicable.Evidence);
        Assert.Empty(inapplicable.Limitations);
        Assert.Contains("explicit", inapplicable.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void AllEvidenceKindsAndArbitraryValueShapesSurvive()
    {
        var model = Read(Load(Valid, WarnEvidence));
        Assert.Equal(Enum.GetValues<FindingEvidenceKind>(), model.Evidence.Select(item => item.Kind));
        Assert.Equal(0, model.Evidence[0].Count);
        Assert.Null(model.Evidence[1].Count);
        Assert.Null(model.Evidence[1].SampleCount);
        Assert.Equal(JsonValueKind.Null, model.Evidence[1].Value.ValueKind);
        Assert.Equal(JsonValueKind.String, model.Evidence[2].Value.ValueKind);
        Assert.Equal(JsonValueKind.Object, model.Evidence[3].Value.ValueKind);
        Assert.Equal(JsonValueKind.False, model.Evidence[4].Value.ValueKind);
        Assert.Equal(JsonValueKind.Array, model.Evidence[5].Value.ValueKind);
        Assert.Equal(JsonValueKind.Undefined, model.Evidence[6].Value.ValueKind);
    }

    [Fact]
    public void EvidenceNumbersRetainPrecisionBeyondDouble()
    {
        var input = JsonNode.Parse(Load(Valid, FailEvidence).GetRawText())!;
        input[Evidence]![0]![Value] = JsonNode.Parse(PreciseNumber);
        var output = Write(Read(JsonSerializer.SerializeToElement(input)));
        Assert.True(Evaluate(output).IsValid);
        Assert.Equal(PreciseNumber, output.GetProperty(Evidence)[0].GetProperty(Value).GetRawText());
    }

    [Fact]
    public void AdditiveRootAndEvidenceFieldsAreIgnoredWithoutLosingKnownValues()
    {
        var original = Load(Valid, WarnEvidence);
        var additive = JsonNode.Parse(original.GetRawText())!;
        additive[FutureField] = new JsonObject { [Value] = true };
        additive[Evidence]![0]![FutureField] = new JsonArray(1, 2);
        var input = JsonSerializer.SerializeToElement(additive);
        Assert.True(Evaluate(input).IsValid);
        var output = Write(Read(input));
        Assert.True(Evaluate(output).IsValid);
        Assert.True(JsonElement.DeepEquals(original, output));
    }

    [Theory]
    [InlineData(CheckVersion)]
    [InlineData(Count)]
    public void IntegerOverflowFailsAtReaderBoundary(string member)
    {
        var input = JsonNode.Parse(Load(Valid, FailEvidence).GetRawText())!;
        var owner = member == CheckVersion ? input : input[Evidence]![0]!;
        owner[member] = JsonNode.Parse("9223372036854775808");
        var element = JsonSerializer.SerializeToElement(input);
        Assert.True(Evaluate(element).IsValid);
        Assert.Throws<JsonException>(() => Read(element));
    }

    [Fact]
    public void EveryInvalidPayloadHasAnExpectation()
    {
        var payloads = Directory.GetFiles(FixtureDirectory(Invalid), "*.json")
            .Select(Path.GetFileName).Where(file => file != Expectations).Order(StringComparer.Ordinal);
        Assert.Equal(payloads, InvalidFixtures.Select(row => (string)row[0]).Order(StringComparer.Ordinal));
    }

    private static FindingV1 Read(JsonElement input) =>
        JsonSerializer.Deserialize(input, FindingJsonContext.Default.FindingV1)!;

    private static JsonElement Write(FindingV1 model) =>
        JsonSerializer.SerializeToElement(model, FindingJsonContext.Default.FindingV1);

    private static EvaluationResults Evaluate(JsonElement input) => Schemas.Evaluate(ContractNames.Finding, input);

    private static string FixtureDirectory(string category) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "CheckResult", "v1", category);

    private static JsonElement Load(string category, string file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixtureDirectory(category), file)));
        return document.RootElement.Clone();
    }

    private static void AssertInvalidAt(EvaluationResults result, string instanceLocation, string keyword)
    {
        Assert.False(result.IsValid);
        Assert.NotNull(result.Details);
        Assert.Contains(result.Details, detail =>
            detail.InstanceLocation.ToString() == instanceLocation && detail.Errors?.ContainsKey(keyword) == true);
    }

    private sealed record FixtureExpectation(string File, string Pointer, string Keyword, bool ReaderRejects);
}
