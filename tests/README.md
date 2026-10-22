# Test foundation

Status: Informative; implemented foundation under explicit user authorization for Proposed [ADR-024](../docs/decisions/ADR-024-contract-test-foundation.md).

The solution contains two .NET 10 test executables. `ScaleProof.UnitTests` checks
the eight production project declarations, compiled assembly references, and
forbidden namespace dependencies. `ScaleProof.ContractTests` builds the three
checked-in schemas in a local registry and verifies nested finding resolution,
format validation, unknown-reference refusal, and the schema inventory snapshot.
Scenario v1 DTOs now have schema, serialization, and reviewed wire snapshot tests.
Production hosts are not started. Validators, runtime workflows, integration,
frontend, and end-to-end tests are not implemented by this foundation.

## Run locally

From the repository root, after installing the SDK selected by `global.json`:

```sh
rtk proxy dotnet restore ScaleProof.sln --locked-mode
rtk proxy dotnet test tests/ScaleProof.UnitTests/ScaleProof.UnitTests.csproj --no-restore
rtk proxy dotnet test tests/ScaleProof.ContractTests/ScaleProof.ContractTests.csproj --no-restore
rtk proxy dotnet build ScaleProof.sln --no-restore
rtk proxy git diff --check
```

These commands use VSTest with the pinned xUnit v3 adapter. MSBuild and VSTest
require local IPC sockets; a restricted execution sandbox may need permission
to run these commands. Restore may access NuGet. Tests use output-local project
copies, schemas, fixtures, and snapshots; they require no internet or services.
`AppContext.BaseDirectory` makes data lookup independent of the working directory.

Direct versions live in each test project; `packages.lock.json` records resolved
transitive versions. For an intentional update, change the project pin, restore
without locked mode, inspect the lock diff, update ADR-024's version table, and
rerun the affected checks. Do not silently update packages to make restore pass.

## Boundaries and controls

The project checks inspect declared references even when a production assembly
contains no types. They reject forbidden references rather than requiring every
allowed edge to be used. Assembly and NetArchTest checks inspect compiled output;
test-only positive/negative controls establish that the scanner catches a real
forbidden namespace dependency on .NET 10. No forbidden production reference is
added for a control. Registration-only Infrastructure access still needs review.
Project declaration checks cover the current direct csproj layout; future imported
or generated dependency declarations must extend these checks rather than rely
on unused references appearing in compiled metadata.

Repeated project/dependency names, XML identifiers, and JSON field names are named
constants shared by the relevant test helpers. Control namespaces come from their
types, and derived names reuse the shared definitions.

`LocalSchemas` builds finding, scenario, then report using a private registry and
draft 2020-12. Its fetch callback throws for an unregistered URI; it never downloads
schemas or reads arbitrary referenced files. Format assertions are enabled.
The report fixture includes a valid nested finding, then tests corrupt its status
and assert failure at `/findings/0/status`. A valid root alone could miss broken
external-reference resolution. HTTP and file URI refusal controls exercise the
same registry. The foundation fixture is intentionally only schema evidence,
with illustrative hashes and no claim that an experiment was run or is runnable.

## Review snapshots deliberately

The initial Verify baseline records schema IDs, sorted root required fields, and
root unknown-property behavior. It is an inventory check, not full-schema drift
coverage. Scenario wire snapshots accompany the v1 fixtures; finding/report DTO coverage
arrives in later tasks.

Reviewed baselines live in `ScaleProof.ContractTests/Snapshots/` and are copied to
the test output's `Snapshots/` directory. On a mismatch, Verify writes a
`*.received.txt` there and fails. Compare the received and verified files, inspect
the relevant schema/spec changes, and only then copy the reviewed result to the
source baseline. Rerun the contract suite. Tests disable graphical diff launching;
no automatic snapshot acceptance is configured. Do not commit received files.

The initial baseline was reviewed against the checked-in schemas. A deliberately
altered output copy of the scenario unknown-property flag caused the targeted
snapshot test to fail; the reviewed baseline was restored afterward. This proves
that unexpected drift fails, not that every contract rule has test coverage.

See the [testing strategy](../docs/engineering/testing-strategy.md) and
[ADR-024 coverage matrix](../docs/decisions/ADR-024-contract-test-foundation.md#requirement-and-story-coverage)
for remaining feature and safety verification.

## Scenario v1 contracts (M0-P01-T03)

`ScaleProof.Contracts.Scenarios.ScenarioV1` and `ScenarioJsonContext.Default`
provide the wire model and generated serialization metadata. Properties have
explicit JSON names; wire enums use their exact string spelling and reject
numeric input. The context rejects unknown object members and null non-nullable
properties. Header and parameter converters distinguish literals from typed
secret/dataset references; they never resolve references. Arbitrary body JSON,
JSON assertion values, check configuration, and numeric threshold values retain
`JsonElement` data. Integer fields use signed 64-bit values; overflow is a reader
failure even if the unconstrained schema integer range would permit the token.

Validate with the scenario schema **before** deserialization. The DTO reader is
not a schema validator: conditional profile requirements, constants (including
schema version), regexes, ranges, collection sizes, and assertion/body `oneOf`
rules belong to the schema boundary. Literal secrets, destination safety,
execution admission, units, effective limits, defaults, and canonicalization
remain later validation/runtime work. No HTTP error mapping exists here.

Optional members remain nullable and are omitted when absent. No workload,
request weight, capture, report, dataset-pick, or mutation defaults are applied.
`JsonElement` uses `Undefined` for an omitted arbitrary value and `Null` for an
explicit JSON null. Empty arrays/objects and explicit `false` survive round trips.
Reserved steps/write methods and capture modes prove wire support only.

Five synthetic inputs live under `fixtures/scenario/v1/valid/`; invalid payloads
live under `invalid/` with an `expectations.json` manifest outside each payload.
The manifest names the schema failure keyword and instance JSON Pointer, intended
future error code, and whether the DTO reader must also reject it. For unknown
fields, `pointer` locates the containing object (the schema diagnostic location)
and `fieldPointer` locates the unexpected field. Expected codes are documentation,
not evidence of an implemented API response. Profile-specific fixtures remove
each required arrival-rate and steps field separately.

Tests validate the input and output schemas and compare JSON trees so that every
known fixture value survives. Verify snapshots store the serialized JSON as raw
text: Verify's object normalization can hide empty collections, so it is avoided
for these wire baselines. Precision tests retain numeric tokens beyond `double`
precision in parameters and thresholds. Review source snapshots against their
input fixtures before accepting a change. Neither snapshots nor schema checks
establish authorization, cancellation, idempotency, traffic safety, or redaction
of runtime secrets; no traffic or secret values enter this task.
