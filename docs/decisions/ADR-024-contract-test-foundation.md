# ADR-024: Contract and architecture test foundation

Status: Proposed
Date: 2026-10-18
Deciders: Repository maintainers; implementation authorized on 2026-10-18, formal status merge pending
Related: FR-001, FR-004, FR-020, FR-023, FR-030; US-001, US-023, US-024; [ADR-003](ADR-003-evidence-contract.md), [ADR-005](ADR-005-versioned-reports.md), [ADR-008](ADR-008-versioned-immutable-contracts.md)

## Context

The solution has eight .NET 10 scaffold projects and three repository-owned JSON
Schemas, but no test projects or package-version configuration. Before runtime
implementation, contributors need executable checks for public wire shapes,
serialization, missing evidence, and project boundaries. DTO round trips alone
cannot prove JSON Schema compliance; snapshots alone can preserve incorrect data.

The [testing strategy](../engineering/testing-strategy.md) already names xUnit v3,
Verify, JsonSchema.Net, and an architecture-testing library. This proposal selects
the initial projects, versions, runner, and test responsibilities. It introduces
no runtime dependency, business-validation framework, or new product requirement.
Approval of the phase or this document's preparation does not accept this ADR.

## Decision drivers

- Match the existing .NET 10 solution, nullable analysis, and warnings-as-errors.
- Keep structural validation, serialization, and domain decisions distinct.
- Detect unsupported versions, lossy DTOs, and missing evidence without traffic.
- Resolve schemas locally and make fixture results deterministic.
- Keep the dependency graph observable even while some assemblies have no types.
- Make acceptance evidence reviewable without machine-local planning files.

## Options considered

| Option | Pros | Cons |
|---|---|---|
| Separate unit/architecture and contract projects, using the documented tools | Clear responsibilities; schema and snapshot assertions complement each other; grows into the existing test layout | Two projects and several test-only dependencies |
| One test project using the same tools | Fewer project files initially | Mixes contract fixtures, architecture checks, and future domain tests; diverges from the planned layout |
| Handwritten schema checks and snapshots with xUnit only | Fewer packages | Reimplements draft 2020-12 rules and snapshot review; incomplete validation is easy to mistake for compliance |
| ArchUnitNET instead of NetArchTest | Also permitted by the strategy; richer architecture model | Additional model/API to adopt for the initial dependency rules |
| Microsoft Testing Platform as the initial `dotnet test` runner | Native support in recent xUnit/.NET releases | Changes invocation/configuration from the planned project-path VSTest commands; no present feature needs the migration |

## Decision

Proposed (ADR-024): select the first option with NetArchTest and VSTest. Implement
only after the maintainer decision; record acceptance separately from this proposal.

Implementation authorization: on 2026-10-18 the user approved continuing after
being asked to approve ADR-024 and T02. This explicitly authorizes the test
foundation described here. Formal Accepted status remains pending a maintainer
merge of the status change; no other Proposed ADR is accepted by this approval.

### Projects and dependency pins

Add two non-packable `net10.0` test executables to `ScaleProof.sln`:

| Project | Production references | Responsibility |
|---|---|---|
| `tests/ScaleProof.UnitTests` | All eight projects for architecture inspection; Domain/Application for later unit tests | Check project references, assembly references, and forbidden namespace dependencies; no production host is started |
| `tests/ScaleProof.ContractTests` | Contracts only | Local schema registry, synthetic fixtures, System.Text.Json round trips, explicit assertions, and Verify snapshots |

Initial direct package pins, to be added to test project files in the
implementation task; no package references are added by this proposal:

| Package | Version | Used by | Compatibility basis |
|---|---|---|---|
| `xunit.v3` | `3.2.2` | Both | Supports .NET 8+; v3 tests are executables |
| `Microsoft.NET.Test.Sdk` | `18.0.0` | Both | Provides the VSTest test host with .NET 8-compatible assets |
| `xunit.runner.visualstudio` | `3.1.5` | Both | VSTest adapter supports xUnit v3 and .NET 8+ |
| `NetArchTest.Rules` | `1.3.2` | UnitTests | .NET Standard 2.0 library; assembly inspection must be verified against .NET 10 output |
| `Verify.XunitV3` | `31.17.0` | ContractTests | Has .NET 10 assets and requires xunit.v3.extensibility.core >= 3.2.2 |
| `JsonSchema.Net` | `9.4.1` | ContractTests | .NET 8-compatible assets; draft 2020-12 evaluator |

These are explicit baseline versions, not a claim to use every newest release.
xUnit 3.2.2 and Verify 31.17.0 form a matching documented dependency pair; this
avoids combining Verify's newer xUnit 4 minimum with a 3.x core. NuGet metadata is
compatibility evidence, not a successful restore/build/discovery result. T02 must
verify the complete resolved graph, analyzers, runner, and architecture inspection.
NetArchTest's latest published release is old (2021); retain the selected scope
only if positive and negative controls detect .NET 10 dependencies correctly.

Use exact direct versions, `PrivateAssets="all"`, and committed per-project
`packages.lock.json` files with locked restore in CI. Keep versions in the two
test project files for this small initial set; do not migrate production package
management or introduce an unused `Directory.Packages.props`. Any later pin change
must update this table and the lock files together with affected checks. FsCheck,
integration tools, coverage collectors, and frontend packages remain deferred.

Keep the existing SDK selection in `global.json`. Use VSTest project-path
invocations locally and in CI; do not opt into the MTP runner or enable its
`dotnet test` bridge. The implementation must demonstrate nonzero test discovery
and a deliberately failing control before claiming the runner is working.

### Contract responsibilities

| Boundary | Planned checks | Excluded responsibility |
|---|---|---|
| JSON Schema | Required fields, types, scalar syntax, allowed values, structural alternatives, bounds, scenario unknown fields, finding evidence-or-limitation, report nullable metric reason | Target authorization, actual safety, evaluator correctness, or persistence |
| DTO reader/writer | Explicit JSON names; string enums with integer values rejected; required versus optional and nullable values; schema version guard; known-value round trips; report additive fields at every object depth | Defaults, target-policy lookup, canonicalization, hashes, limits, or result computation |
| Domain/Application, deferred | Unique request names; resolved placeholders; supported metric/unit pairs; limit/default resolution; secret detectors; destination policy; lifecycle/quality/outcome consistency | These rules must not become DTO setters or constructors |

Use System.Text.Json metadata in Contracts, with converters only for wire forms
that need them. Required presence and nullable value are separate: C# `required`
alone does not reject explicit JSON null. Test missing and null inputs separately.
Missing required numbers/enums must fail rather than materialize as zero or pass.
Optional/defaultable inputs preserve omission until the later validation boundary;
JSON Schema `default` annotations do not authorize DTO default resolution.

Scenario readers reject unknown structural fields, while intentional dictionaries
and arbitrary JSON payloads follow their schema. Report readers ignore additive
v1 properties, including nested ones; unknown fields need not survive a round trip.
Unsupported schema versions fail explicitly. Fixtures use synthetic identifiers,
timestamps, and illustrative hashes, with no hash computation or execution implied.

Test flow: load a versioned fixture, validate it against local schemas, deserialize
valid input, assert known values, serialize, validate output, then compare the
snapshot. Invalid fixtures name the expected boundary and instance JSON Pointer;
schema failures are not asserted as API Problem Details codes. Snapshot review is
manual; CI never auto-accepts received files. Avoid scrubbing contract IDs, nulls,
timestamps, or hashes in ways that could hide wire regressions.

### Offline schema and architecture checks

Register all three checked-in schemas by their exact `$id` in one shared contract
test helper. Resolve report findings to the local check-result schema. Unknown
references must fail without an HTTP fallback; the HTTPS IDs are identifiers,
not instructions to fetch. Copy schemas and fixtures to test output so tests do
not depend on the current working directory. Enable format assertions for the
formats under test and test malformed values; a `date-time` format alone does
not establish the report's stronger UTC rule.

Architecture tests inspect the actual project-reference graph as well as compiled
assemblies/types: a type-only test over empty scaffolds could pass vacuously.
Compare the eight projects against the documented graph, enforce Domain/Contracts
isolation and the Agent's forbidden server/EF dependencies, and prohibit forbidden
namespace dependencies. Use test-only negative controls to demonstrate detection
without adding forbidden references to production. Registration-only
Infrastructure use in composition roots also requires code review.

The current scaffold graph matches the
[blueprint graph](../architecture/implementation-blueprint.md#dependency-rules),
but enforcing that graph does not authorize Proposed ADR-022 runtime ownership.
Accepted ADR-002/009 versus Proposed ADR-022 remains a separate decision. This ADR
does not accept ADR-013/014/015/018/022/023 or implement their algorithms/protocols.

### Requirement and story coverage

These are planned contract assertions, not completion claims for full features.

| Reference | Phase 1 evidence | Still requires later slices |
|---|---|---|
| FR-001 | Scenario version marker; report scenario/target/policy identities and illustrative hashes survive serialization | Version allocation, immutable persistence, canonicalization, actual hashes |
| FR-004 | Secret-reference wire forms round-trip; synthetic fixtures contain no real secrets; fixture secret scan in CI | Literal detection, redaction in every runtime output, credential resolution |
| FR-020 | Units/source/window/sample count/definition version retained; unavailable metric is null with a reason; client/server metrics remain distinct | Engine normalization, aggregation, adequacy and quality algorithms |
| FR-023 | All seven statuses; independent severity/confidence; check ID/version, evidence, limitations, recommendations; evidence-or-limitation rule | Pure evaluators, heuristic evidence sufficiency, effective recommendations |
| FR-030 | Versioned JSON report, partial/redaction metadata, nested findings, stable serialization | Retained evidence, reproducible report generation, Markdown rendering |
| US-001 | Schema-invalid fixtures identify actionable locations; schema-valid input is not treated as traffic authorization | Validation API, semantic/policy/secret checks, zero-traffic integration proof |
| US-023 | Distinct fail/error/inconclusive/not_applicable/skipped forms; evidence rule follows the normative finding spec | Correct status selection by real evaluators |
| US-024 | Evidence references, limitations, redaction/unavailable reasons, recommendations and check version survive serialization | Evidence retrieval/pagination, authorization, artifact resolution and UI drill-down |

Authorization, cancellation, concurrency, idempotency and SSRF tests are not
applicable to these DTO-only checks. They remain mandatory in the relevant runtime
slices. Fixture hygiene does not establish runtime redaction.

### Discrepancies to resolve with the affected contracts

- US-023 says every non-pass outcome needs evidence or an unavailable reason;
  the normative [finding spec](../specs/finding-v1.md#field-semantics) and schema
  exempt `not_applicable` and `skipped` as well as `pass`. Follow the normative
  exemptions in finding fixtures; align the lower-precedence story in that task.
- The [scenario field table](../specs/scenario-v1.md#field-reference) describes
  smoke `iterations` and `vus` with a five-VU maximum. The schema's smoke branch
  requires only `iterations` and its shared `vus` has no smoke-specific maximum.
  Resolve requiredness and the structural cap with schema regression fixtures
  during the scenario task; do not silently move the discrepancy into DTO logic.
- Report `date-time` accepts non-UTC offsets although the
  [report rules](../specs/report-v1.md#rules) require UTC. Assert the stronger
  wire rule in report reader tests and document any schema correction there.

## Consequences

Contributors gain two focused test projects without runtime packages. Schema,
explicit value assertions, and snapshots detect different classes of defect.
Lock files make the resolved graph reviewable, while updates require deliberate
maintenance. Static architecture checks cannot establish all behavioral boundaries.

The implementation task must prove runner discovery, positive/negative dependency
checks, and local report-to-finding resolution before DTO tasks rely on the tools.
Later contract tasks add meaningful valid/invalid fixtures rather than placeholders.
No public schema, source project, package configuration, or Accepted ADR changes
as a consequence of merely proposing this document.

## Revisit when

- Any selected package cannot restore/build/run with the supported .NET 10 SDK
  under warnings-as-errors, or a negative architecture control is missed.
- A security advisory affects a locked dependency, or a required fix needs a new
  package version; revalidate compatibility before updating pins.
- The test-project count exceeds four or duplicated package pins diverge: evaluate
  central package management in a follow-up decision.
- An accepted feature requires MTP-only tooling: reconsider the runner with local
  and CI discovery/failure evidence before migrating commands.

## Primary compatibility resources

Checked 2026-10-18; package metadata was inspected, packages were not installed.

- [xUnit 3.2.2 metadata](https://www.nuget.org/packages/xunit.v3/3.2.2) and
  [xUnit setup](https://xunit.net/docs/getting-started/v3/getting-started): framework floor, executable tests, and runner alternatives.
- [Verify.XunitV3 31.17.0 metadata](https://www.nuget.org/packages/Verify.XunitV3/31.17.0): .NET 10 asset group and xUnit core dependency floor.
- [VSTest adapter 3.1.5](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.5) and
  [Test SDK 18.0.0](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.0.0): test-host integration and compatible framework assets.
- [JsonSchema.Net 9.4.1](https://www.nuget.org/packages/JsonSchema.Net/9.4.1) and
  [evaluator documentation](https://docs.json-everything.net/schema/basics/): schema evaluation, diagnostics, and format handling.
- [NetArchTest 1.3.2](https://www.nuget.org/packages/NetArchTest.Rules/1.3.2) and
  [source repository](https://github.com/BenMorris/NetArchTest): framework target and fluent dependency assertions.
- [Microsoft test-runner documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test): VSTest versus MTP selection in `dotnet test`.
