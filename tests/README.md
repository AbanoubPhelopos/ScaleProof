# Test foundation

Status: Informative; implemented foundation under explicit user authorization for Proposed [ADR-024](../docs/decisions/ADR-024-contract-test-foundation.md).

The solution contains two .NET 10 test executables. `ScaleProof.UnitTests` checks
the eight production project declarations, compiled assembly references, and
forbidden namespace dependencies. `ScaleProof.ContractTests` builds the three
checked-in schemas in a local registry and verifies nested finding resolution,
format validation, unknown-reference refusal, and the schema inventory snapshot.
Scenario, finding, and report v1 DTOs have schema, serialization, and reviewed
wire snapshot tests.
Production hosts are not started. Validators, runtime workflows, integration,
frontend, and end-to-end tests are not implemented by this foundation.

## Run locally

From the repository root, after installing the SDK selected by `global.json`:

```sh
rtk proxy python3 -B -m unittest discover -s scripts/tests -v
rtk proxy python3 -B scripts/check_fixture_secrets.py
rtk proxy dotnet restore ScaleProof.sln --locked-mode
rtk proxy dotnet build ScaleProof.sln --configuration Release --no-restore -warnaserror
rtk proxy dotnet test tests/ScaleProof.UnitTests/ScaleProof.UnitTests.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=unit.trx" --results-directory "$PWD/artifacts/test-results/unit"
rtk proxy dotnet test tests/ScaleProof.ContractTests/ScaleProof.ContractTests.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=contracts.trx" --results-directory "$PWD/artifacts/test-results/contracts"
rtk proxy git diff --check
```

These commands match CI flags and use VSTest with the pinned xUnit v3 adapter.
`rtk proxy` is the local command wrapper; CI runs the underlying commands directly.
Python 3 is required for the standard-library hygiene script/tests; no pip packages
are needed. Build Release before using `--no-build`. MSBuild and VSTest
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
coverage. Scenario, finding, and report wire snapshots accompany their v1 fixtures.

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

## Finding v1 contracts (M0-P01-T04)

`ScaleProof.Contracts.Findings.FindingV1` and `FindingJsonContext.Default` model
check-result v1, using separate status, severity, confidence, and evidence-kind
enums. Required members have no pass/empty-array defaults. The context uses
explicit JSON names, string enums without numeric fallback, and nullable-member
checks. As the existing finding schema permits additional properties, additive
root and evidence fields are ignored. Known values survive reserialization;
ignored future fields need not survive. Unsupported schema versions are rejected
by schema validation, which must precede reading the DTO.

`FindingEvidence` carries the schema's optional fields without imposing extra
per-kind requirements. Its arbitrary `value` retains JSON tokens and precision;
omitted values use `Undefined`, explicit JSON null uses `Null`. Nullable counts
preserve absence versus explicit zero. Counts/check versions use signed Int64;
overflow is a documented reader failure even for a schema-valid integer token.

Fixtures live in `fixtures/check-result/v1/{valid,invalid}/`. Eleven valid inputs
cover all seven statuses, all seven evidence kinds, meaningful missing-evidence
limitations, and explicit inapplicability. Twenty invalid payloads and their
`expectations.json` identify schema diagnostic locations/keywords and required
reader failures. These are finding validation assertions, not API error mappings.
Raw JSON snapshots are reviewed against the complete input trees. Additional
tests cover all 15 severity/confidence pairs, every required root field,
precision, additive fields, and Int64 overflow.

For fail/warn/inconclusive/error, tests independently prove that evidence alone
or a limitation alone satisfies the schema; omitting both fails validation. The other
three statuses may have empty evidence/limitations. The reader itself does not
enforce that conditional rule. Schema validation cannot judge whether limitation
text truly explains unavailable evidence or whether a check is heuristic; those
are future evaluator obligations. Missing telemetry remains inconclusive in its
fixture; not_applicable has an explicitly known synthetic precondition. No check
evaluator, telemetry interpretation, reference resolution, I/O, authorization,
cancellation, idempotency workflow, or runtime redaction is implemented here.
The documentation reference in the synthetic fixture is illustrative.

## Report v1 contracts (M0-P01-T05)

`ScaleProof.Contracts.Reports.ReportV1` and `ReportJsonContext.Default` model the
report wire contract with generated metadata and nested `FindingV1` objects.
Lifecycle, quality status, outcome, threshold status, and finding status remain
separate types. No report field computes measurements, thresholds, quality,
comparison eligibility, deltas, or verdicts. Build identity, comparison payloads,
and quality-reason entries retain JSON because their schema shapes are open.
Known documented fields such as environment agentId and threshold request scope
are modeled; other additive typed-object properties are ignored at every level.

Required nullable quality/outcome fields always write explicit null when unknown.
Metric values and threshold observations use required `JsonElement` members to
preserve numeric tokens or null without inventing zero. Optional rate/build/
comparison JSON retains omission versus explicit null. `ReportOptional<T>` is a
small serialization value type used for optional nullable timestamps, dropped
iteration counts, percentile status, and unavailable reasons. Its default means
absent; constructing it (including with null) marks it present. Closed generic
property converters write just the underlying JSON value, not wrapper metadata.
No execution defaults are applied. Ordinary optional non-nullable wire fields
remain nullable C# members and omit null; schema validation rejects explicit null
for those fields before reading.

The report schemaVersion property converter rejects unsupported version strings
on reading and writing. Timestamp metadata uses DateTimeOffset and a UTC converter
at all typed timestamp positions, including optional nullable ones: explicit Z or
+00:00 is required; output uses Z. Instants retain .NET tick precision (100 ns),
not the input's lexical formatting. Counters/versions/durations/bytes use Int64;
overflow is a reader failure. Numeric measurements retain raw JSON precision.

Existing schema/prose gaps are explicit: schema `date-time` permits non-UTC
offsets while normative report prose requires UTC; the reader enforces the prose.
Threshold metric/aggregation/operator/value/unit and artifact kind/key/mediaType
are required but not type-constrained in the current schema. DTOs model the
string/JSON values from the documented shape; schema validation alone cannot
prove a report fits every reader or every semantic invariant. The schema remains
unchanged. Cross-field lifecycle/quality/outcome policy, diagnostic-tunnel
limitations, metric adequacy, and measurement-window rules remain later producer/
validator obligations. No new error-code API mapping exists.

Eight valid fixtures under `fixtures/report/v1/valid/` cover completed, failed,
cancelled before start, partial evidence, all unavailable reasons, explicit zero,
nested findings, fixed-threshold comparison data, and reserved locations/profiles.
Reserved wire data is not runtime authorization. Comparison fixture data is
synthetic; no comparison or Proposed statistics algorithm is implemented.
Nineteen invalid payloads and `invalid/expectations.json` identify schema failure
locations/keywords and reader failures. Non-UTC cases are marked schema-valid
because they fail the stricter normative reader boundary.

Tests validate both input/output schemas with offline finding resolution, compare
complete known JSON trees, and review raw serialization snapshots. Additive fields
are tested at 21 typed root/nested object locations. Required root fields, missing
metric values/reasons, null versus omission, precision, unsupported versions,
UTC/timestamp formatting, invalid nested findings, and Int64 overflow are covered.
These are contract checks, not report rendering, engine ingestion, runtime
redaction, traffic, authorization, cancellation, or idempotency acceptance tests.

## Contract foundation CI (M0-P01-T06)

[The workflow](../.github/workflows/contract-foundation.yml) runs on pull requests
whose base is master and on pushes to master, without path filters. It runs the
commands above on Ubuntu 24.04 after installing the SDK selected by global.json.
The existing rollForward policy remains in force; this is not an assertion that
all machines use an identical SDK patch. Actions are pinned to full upstream
commit SHAs (major-version comments identify the selected release lines).
Updating an action requires reviewing the upstream change and replacing its pin.
The workflow neither installs RTK nor configures repository branch protection.

The job has contents:read permission, disables persisted checkout credentials,
and passes no repository secrets to test commands. Fixture hygiene runs before
restore/build/tests. A failure there stops subsequent normal steps and prevents
diagnostic artifact upload. After a successful build, both .NET suites run even
if the first suite fails; each failure still makes the job fail. Cancelled runs
skip remaining test/upload work. Repeated runs for the same ref cancel older runs.

The diagnostics artifact retains separate unit/contracts TRX reports and the
contract output's received/verified snapshot files for seven days, including on
test failure. Console diagnostics remain in workflow logs. Paths are restricted
to test results and snapshot files; binaries, entire workspaces and environment
files are not uploaded. No-files-found warns rather than hiding an earlier build
failure. A hosted workflow run can only be confirmed after the workflow reaches
a triggering pull request/push; local execution/lint is not a hosted run.

Snapshot mismatches fail. No received file is copied to a source baseline by CI,
and no automatic Verify acceptance is configured. Download diagnostics, compare
received/verified values with the relevant spec/schema and fixture, then review
and intentionally update the source baseline as described above. Schema coverage
is the implemented schema/fixture assertions plus the partial inventory snapshot,
not a claim that every possible schema change is automatically detected.

### Synthetic fixture hygiene

[`check_fixture_secrets.py`](../scripts/check_fixture_secrets.py) scans all JSON
files under tests/fixtures and all reviewed *.verified.txt source snapshots. It
checks common private-key/token/authorization/password patterns plus decoded JSON
credential fields. JSON escapes are decoded, so escaped token strings and keys
are inspected too. The initial non-JSON schema-inventory snapshot receives raw
text pattern checks. Secret-reference objects with Bearer {value} format templates,
synthetic hashes, numeric precision examples and artifact keys are allowed without
blanket path exclusions. Reference objects still have their nested fields scanned.

Matches fail with filenames and rule names, never matched input values. Invalid
fixture JSON, unreadable UTF-8, missing/empty scopes and symlinks also fail. The
script locates its root from its own path; --root is available for synthetic test
controls. Python's -B keeps bytecode out of the checkout. Fifteen standard-library
unit tests cover positive/negative patterns, escapes, safe reporting and failure
conditions using temporary files and constructed synthetic canaries.

This is bounded fixture hygiene, not an exhaustive secret/PII detector, runtime
redaction implementation or substitute for reviewing synthetic data. No check
rewrites data or accepts a snapshot. Hosted setup/actions and package restore may
use network access; the scanner and .NET test suites require no external service.
CI does not run absent integration, k6, frontend or end-to-end checks.
