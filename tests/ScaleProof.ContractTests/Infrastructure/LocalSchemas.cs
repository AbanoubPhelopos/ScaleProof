using System.Text.Json;
using Json.Schema;

namespace ScaleProof.ContractTests.Infrastructure;

internal sealed class LocalSchemas
{
    internal const string UnregisteredSchemaError = "Schema is not registered locally:";

    private readonly BuildOptions options = new()
    {
        Dialect = Dialect.Draft202012,
        SchemaRegistry = new SchemaRegistry
        {
            Fetch = (uri, _) => throw new InvalidOperationException($"{UnregisteredSchemaError} {uri}"),
        },
    };

    private readonly Dictionary<string, JsonSchema> schemas = new(StringComparer.Ordinal);

    internal LocalSchemas()
    {
        // Build the referenced finding schema first; never modify global registries.
        foreach (var contract in ContractNames.BuildOrder)
        {
            var path = SchemaPath(contract);
            schemas.Add(contract, JsonSchema.FromFile(path, options));
        }
    }

    internal EvaluationResults Evaluate(string contract, JsonElement instance) =>
        schemas[contract].Evaluate(instance, EvaluationSettings());

    internal JsonSchema Build(string json) => JsonSchema.FromText(json, options);

    internal static EvaluationOptions EvaluationSettings() => new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true,
    };

    internal static string SchemaPath(string contract) =>
        Path.Combine(AppContext.BaseDirectory, "Schemas", contract, "v1.schema.json");
}
