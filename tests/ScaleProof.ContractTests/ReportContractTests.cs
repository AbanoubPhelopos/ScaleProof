using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using ScaleProof.Contracts.Reports;
using ScaleProof.ContractTests.Infrastructure;
using VerifyXunit;

namespace ScaleProof.ContractTests;

public sealed class ReportContractTests
{
    private const string Valid = "valid";
    private const string Invalid = "invalid";
    private const string Expectations = "expectations.json";
    private const string Completed = "completed.json";
    private const string BeforeStart = "cancelled-before-start.json";
    private const string Metrics = "metrics";
    private const string Value = "value";
    private const string Run = "run";
    private const string Outcome = "outcome";
    private const string QualityStatus = "qualityStatus";
    private const string UnavailableReason = "unavailableReason";
    private const string GeneratedAt = "generatedAt";
    private const string OfferedRate = "offeredRate";
    private const string UnsupportedVersion = "scaleproof.report/v2";
    private const string StartedAt = "startedAt";
    private const string FinishedAt = "finishedAt";
    private const string Workload = "workload";
    private const string DroppedIterations = "droppedIterations";
    private const string PercentileStatus = "percentileStatus";
    private const string Required = "required";
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
                expectation.File, expectation.Pointer, expectation.Keyword, expectation.ReaderRejects, expectation.SchemaValid,
            });

    public static IEnumerable<object[]> RequiredRootFields
    {
        get
        {
            using var document = JsonDocument.Parse(File.ReadAllText(LocalSchemas.SchemaPath(ContractNames.Report)));
            return document.RootElement.GetProperty(Required).EnumerateArray()
                .Select(member => new object[] { member.GetString()! }).ToArray();
        }
    }

    private static readonly string[] TypedLocations =
    [
        "", "/tool", "/run", "/identity", "/identity/scenario", "/identity/target", "/identity/policy",
        "/identity/datasets/0", "/identity/engine", "/identity/environment", "/workload", "/workload/windows/0",
        "/metrics/0", "/perRequest/0", "/perRequest/0/metrics/0", "/thresholds/0", "/quality", "/redaction",
        "/artifacts/0", "/findings/0", "/findings/0/evidence/0",
    ];

    public static IEnumerable<object[]> TypedObjectLocations => TypedLocations.Select(location => new object[] { location });

    [Theory]
    [MemberData(nameof(ValidFixtures))]
    public void ValidFixturesSatisfySchemaAndPreserveEveryKnownValue(string file)
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
        var output = JsonSerializer.Serialize(Read(Load(Valid, file)), ReportJsonContext.Default.ReportV1);
        await Verifier.Verify(output)
            .UseParameters(Path.GetFileNameWithoutExtension(file))
            .UseDirectory(Path.Combine(AppContext.BaseDirectory, "Snapshots"))
            .DisableDiff();
    }

    [Theory]
    [MemberData(nameof(InvalidFixtures))]
    public void InvalidFixturesFailAtDocumentedBoundary(
        string file, string instanceLocation, string keyword, bool readerRejects, bool schemaValid)
    {
        var input = Load(Invalid, file);
        var result = Evaluate(input);
        Assert.Equal(schemaValid, result.IsValid);
        if (!schemaValid)
        {
            Assert.NotNull(result.Details);
            Assert.Contains(result.Details, detail =>
                detail.InstanceLocation.ToString() == instanceLocation && detail.Errors?.ContainsKey(keyword) == true);
        }
        if (readerRejects)
        {
            Assert.Throws<JsonException>(() => Read(input));
        }
    }

    [Theory]
    [MemberData(nameof(RequiredRootFields))]
    public void EveryRequiredRootFieldIsRequiredBySchemaAndReader(string member)
    {
        var input = JsonNode.Parse(Load(Valid, Completed).GetRawText())!.AsObject();
        Assert.True(input.Remove(member));
        var element = JsonSerializer.SerializeToElement(input);
        Assert.False(Evaluate(element).IsValid);
        Assert.Throws<JsonException>(() => Read(element));
    }

    [Theory]
    [MemberData(nameof(TypedObjectLocations))]
    public void AdditiveFieldsAtEveryTypedLevelAreIgnored(string location)
    {
        var original = Load(Valid, Completed);
        var input = JsonNode.Parse(original.GetRawText())!;
        var owner = input;
        foreach (var segment in location.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            owner = owner is JsonArray array ? array[int.Parse(segment, System.Globalization.CultureInfo.InvariantCulture)]! : owner[segment]!;
        }
        owner["futureField"] = new JsonObject { [Value] = 1 };
        var element = JsonSerializer.SerializeToElement(input);
        Assert.True(Evaluate(element).IsValid);
        var output = Write(Read(element));
        Assert.True(Evaluate(output).IsValid);
        Assert.True(JsonElement.DeepEquals(original, output));
    }

    [Fact]
    public void MissingEvidenceRemainsNullWithoutInventingAPassingOutcome()
    {
        var report = Read(Load(Valid, BeforeStart));
        Assert.Equal(ReportLifecycle.Cancelled, report.Run.Lifecycle);
        Assert.Null(report.Run.QualityStatus);
        Assert.Null(report.Run.Outcome);
        Assert.Null(report.Quality.Status);
        var output = Write(report);
        Assert.Equal(JsonValueKind.Null, output.GetProperty(Run).GetProperty(QualityStatus).ValueKind);
        Assert.Equal(JsonValueKind.Null, output.GetProperty(Run).GetProperty(Outcome).ValueKind);
        var partial = Read(Load(Valid, "cancelled-partial.json"));
        Assert.Equal(ReportLifecycle.Cancelled, partial.Run.Lifecycle);
        Assert.Equal(ReportQualityStatus.Inconclusive, partial.Run.QualityStatus);
        Assert.Null(partial.Run.Outcome);
        Assert.Equal(JsonValueKind.Null, partial.Metrics[0].Value.ValueKind);
        Assert.Equal(ReportUnavailableReason.InsufficientSamples, partial.Metrics[0].UnavailableReason.Value);
        Assert.Equal(JsonValueKind.Null, partial.Thresholds[0].Observed.ValueKind);
        Assert.Equal(ReportThresholdStatus.Inconclusive, partial.Thresholds[0].Status);
    }

    [Fact]
    public void AbsentOptionalNullableMembersRemainDistinctFromExplicitNull()
    {
        var omitted = Read(Load(Valid, "reserved-server_worker.json"));
        Assert.False(omitted.Run.StartedAt.IsPresent);
        Assert.False(omitted.Workload.DroppedIterations.IsPresent);
        var explicitNull = Read(Load(Valid, BeforeStart));
        Assert.True(explicitNull.Run.StartedAt.IsPresent);
        Assert.Null(explicitNull.Run.StartedAt.Value);
        Assert.True(explicitNull.Run.FinishedAt.IsPresent);
        Assert.Null(explicitNull.Run.FinishedAt.Value);
        var output = Write(explicitNull);
        Assert.Equal(JsonValueKind.Null, output.GetProperty(Run).GetProperty(StartedAt).ValueKind);
        Assert.Equal(JsonValueKind.Null, output.GetProperty(Run).GetProperty(FinishedAt).ValueKind);
        var input = JsonNode.Parse(Load(Valid, Completed).GetRawText())!;
        input[Metrics]![0]!.AsObject().Remove(UnavailableReason);
        input[Metrics]![0]!.AsObject().Remove(PercentileStatus);
        var restored = Write(Read(JsonSerializer.SerializeToElement(input)));
        Assert.False(restored.GetProperty(Metrics)[0].TryGetProperty(UnavailableReason, out _));
        Assert.False(restored.GetProperty(Metrics)[0].TryGetProperty(PercentileStatus, out _));
    }

    [Fact]
    public void AllUnavailableReasonsAndExplicitZeroArePreserved()
    {
        var report = Read(Load(Valid, "unavailable-metrics.json"));
        Assert.Equal(Enum.GetValues<ReportUnavailableReason>(), report.Metrics.Take(5).Select(metric => metric.UnavailableReason.Value!.Value));
        Assert.All(report.Metrics.Take(5), metric => Assert.Equal(JsonValueKind.Null, metric.Value.ValueKind));
        Assert.Equal(0, report.Metrics[5].Value.GetInt32());
        Assert.True(report.Metrics[5].UnavailableReason.IsPresent);
        Assert.Null(report.Metrics[5].UnavailableReason.Value);
        Assert.Equal(ReportPercentileStatus.Unstable, report.Metrics[6].PercentileStatus.Value);
        Assert.Equal(JsonValueKind.Null, report.Workload.OfferedRate.ValueKind);
        Assert.Equal(JsonValueKind.Null, report.Workload.AchievedRate.ValueKind);
        Assert.True(report.Workload.DroppedIterations.IsPresent);
        Assert.Null(report.Workload.DroppedIterations.Value);
    }

    [Theory]
    [InlineData(GeneratedAt)]
    [InlineData(StartedAt)]
    public void NonUtcAndUnspecifiedInstantsAreRejectedByTheReader(string member)
    {
        foreach (var instant in new[] { "2026-01-01T01:00:00+01:00", "2026-01-01T00:00:00" })
        {
            var input = JsonNode.Parse(Load(Valid, Completed).GetRawText())!;
            (member == GeneratedAt ? input : input[Run]!)[member] = instant;
            Assert.Throws<JsonException>(() => Read(JsonSerializer.SerializeToElement(input)));
        }
    }

    [Fact]
    public void UtcOffsetsAndFractionalInstantsRetainTheirInstantWhenWrittenAsZ()
    {
        const string instant = "2026-01-01T00:00:00.1234567+00:00";
        var input = JsonNode.Parse(Load(Valid, Completed).GetRawText())!;
        input[GeneratedAt] = instant;
        var report = Read(JsonSerializer.SerializeToElement(input));
        var output = Write(report);
        Assert.True(Evaluate(output).IsValid);
        Assert.Equal("2026-01-01T00:00:00.1234567Z", output.GetProperty(GeneratedAt).GetString());
        Assert.Equal(report.GeneratedAt, Read(output).GeneratedAt);
    }

    [Fact]
    public void WriterRejectsUnsupportedVersionsAndNonUtcInstants()
    {
        var report = Read(Load(Valid, Completed));
        Assert.Throws<JsonException>(() => Write(report with { SchemaVersion = UnsupportedVersion }));
        Assert.Throws<JsonException>(() => Write(report with { GeneratedAt = new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.FromHours(1)) }));
    }

    [Fact]
    public void NumericMeasurementsRetainPrecisionBeyondDouble()
    {
        var input = JsonNode.Parse(Load(Valid, Completed).GetRawText())!;
        input[Metrics]![0]![Value] = JsonNode.Parse(PreciseNumber);
        input[Workload]![OfferedRate] = JsonNode.Parse(PreciseNumber);
        var output = Write(Read(JsonSerializer.SerializeToElement(input)));
        Assert.True(Evaluate(output).IsValid);
        Assert.Equal(PreciseNumber, output.GetProperty(Metrics)[0].GetProperty(Value).GetRawText());
        Assert.Equal(PreciseNumber, output.GetProperty(Workload).GetProperty(OfferedRate).GetRawText());
    }

    [Theory]
    [InlineData("reportVersion")]
    [InlineData(DroppedIterations)]
    public void IntegerOverflowIsAReaderFailure(string member)
    {
        var input = JsonNode.Parse(Load(Valid, Completed).GetRawText())!;
        (member == DroppedIterations ? input[Workload]! : input)[member] = JsonNode.Parse("9223372036854775808");
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

    private static ReportV1 Read(JsonElement input) =>
        JsonSerializer.Deserialize(input, ReportJsonContext.Default.ReportV1)!;

    private static JsonElement Write(ReportV1 model) =>
        JsonSerializer.SerializeToElement(model, ReportJsonContext.Default.ReportV1);

    private static EvaluationResults Evaluate(JsonElement input) => Schemas.Evaluate(ContractNames.Report, input);

    private static string FixtureDirectory(string category) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Report", "v1", category);

    private static JsonElement Load(string category, string file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixtureDirectory(category), file)));
        return document.RootElement.Clone();
    }

    private sealed record FixtureExpectation(string File, string Pointer, string Keyword, bool ReaderRejects, bool SchemaValid);
}
