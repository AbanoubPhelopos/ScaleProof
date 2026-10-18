# Data and contracts

Status: Normative for record/version invariants; local-agent ownership and AI records below are Proposed (ADR-022, ADR-023)
Audience: backend implementers, contract authors
Related: [database schema](../specs/database-schema.md) · [API reference](../specs/api-reference.md) · [scenario v1](../specs/scenario-v1.md) · [finding v1](../specs/finding-v1.md) · [report v1](../specs/report-v1.md) · [comparison](../specs/comparison.md)

## Core records

| Record | Essential fields and invariants |
|---|---|
| Project | ID, name/slug, owner/local marker, settings; authorization boundary in hosted mode. |
| TargetProfileVersion | Project, environment class, sanitized base URL, allowed rules, policy reference, version. Immutable once referenced. |
| ScenarioVersion | Project, schema version, canonical definition, hash, version, creator/time. |
| ExecutionPolicyVersion | Limits, target rules, mutation approval, capture rules, version. |
| Run | Project, exact scenario/target/policy versions and hashes, baseline, lifecycle/quality, build/environment identity, timestamps/failure. |
| RunAttempt | Run, ordinal, lease owner/token/expiry, agent/engine versions, lifecycle, timestamps, reconciliation state. |
| MetricSummary | Attempt, name, unit, aggregation, value/null, sample count, bounded dimensions, source/definition version. |
| Artifact | Attempt/run, kind, storage key, content hash, size, media/schema/redaction versions, expiry. |
| CheckResult | Run, check ID/version, status, severity, confidence, evidence, limitations, recommendations. |
| Comparison | Baseline/candidate, compatibility status/reasons, deltas, policy status/version. |
| AuditEvent | Project, actor/mode, event, target/policy reference, time; no secrets or captured bodies. |
| Agent | Project, display name, credential fingerprint/status, protocol/version, capabilities, last seen, revoked time. No private key. |
| AgentSession | Agent, connection/session identity, sequence/ack state, connected/disconnected times, remote metadata sanitized. |
| LocalTargetRuleVersion | Agent/project, exact schemes/hosts/IP scopes/ports/base paths, mutation permission, creator/time, immutable version. |
| ExecutionManifest | Attempt/agent, scenario/target/policy hashes, caps, nonce, issued/expiry time, signing key ID/signature, acceptance status. |
| ProgressEvent | Attempt, monotonically increasing sequence, bounded type/payload, created/expiry time. |
| ProjectQuota | Project, queued/active/daily run limits, agent/artifact/workload budgets, version. |

Use UTC instants and explicit durations. Completed runs/reports are immutable except through versioned redaction/deletion workflows. Content hashes guard artifact integrity. Monetary estimates use decimal values plus currency and allocation metadata.

Initial indexes should support run history, atomic queued-run claims, scenario history, baseline references, metric/check lookups, artifact listing, and project audit history; validate them against real plans.

## API boundary

The API is versioned JSON REST with bounded payloads and RFC 9457 Problem Details errors. The endpoint list, idempotency, pagination, and status-code rules are in [API reference](../specs/api-reference.md). Error codes are in [error codes](../specs/error-codes.md).

Submission idempotency (`Idempotency-Key`) protects ScaleProof's own submission. It is distinct from testing a target application's idempotency.

## Scenario contract

A scenario must declare:

- schema version and stable name;
- approved target-profile reference and optional allowed base path;
- workload engine/profile, warm-up, measurement, ramp-down, timeouts, and hard caps;
- weighted named requests, methods, relative paths, data references, and response assertions;
- thresholds with metric, aggregation, operator, value, and unit;
- checks and versioned configuration;
- environment/mutation classification and abort conditions;
- report formats and capture/redaction policy.

Unknown fields are rejected for v1. Durations use one documented syntax. Secrets are references. Canonicalization is defined before hashing so formatting/property order do not change identity. Resolved request paths and redirects cannot escape target policy.

Canonicalization (details in [scenario v1](../specs/scenario-v1.md#canonicalization-and-hashing); RFC 8785 proposed in [ADR-013](../decisions/ADR-013-rfc8785-canonicalization.md)) parses YAML/JSON into one typed model, applies documented defaults, sorts object properties and unordered sets, preserves ordered arrays, emits UTF-8 canonical JSON, and hashes it with SHA-256. Store the canonical form and canonicalization/hash version.

Example families: read-only smoke, catalog arrival rate, bounded stress steps, concurrency update, sequential/concurrent idempotency, and repeated-query diagnostics. Mutation examples target only the disposable sample app and include reset behavior.

## Finding contract

Required properties: check ID/version, status, severity, confidence, title/summary, structured evidence references, limitations, recommendations, and documentation reference when applicable.

Statuses are `pass`, `fail`, `warn`, `inconclusive`, `not_applicable`, `skipped`, and `error`. Confidence is `low`, `medium`, or `high` and is not a probability. Severity describes potential operational impact, not evidence strength.

## Report contract

A report records run/attempt identity, schema/tool/engine versions, scenario/target/policy hashes, build and environment fingerprint, all workload intervals, offered/achieved load, request/error/timeout counts, latency definitions and sample counts, generator validity, optional resource/DB/queue evidence, findings, compatibility, thresholds, limitations, redaction state, and artifact hashes.

Missing evidence is `null`/unavailable with a reason. Client-observed latency and server-span duration remain separately named. Percentiles with inadequate samples are suppressed or flagged unstable by a versioned policy.

Comparison returns `compatible`, `conditionally_comparable`, or `incompatible` before computing a verdict. It checks scenario and endpoint mix, load shape/rate/duration, target/environment/dataset/cache state, engine compatibility, authentication/configuration, telemetry availability, and generator validity.

Reports record `executionLocation` (`server_worker`, `local_agent`, or `diagnostic_tunnel`) and agent/region/network metadata appropriate for comparison. `diagnostic_tunnel` runs are automatically incompatible with performance baselines and capacity conclusions.

## Contract versioning policy

- Additive optional fields may remain within a schema version only when old readers safely ignore them by documented rule.
- Changed meaning, required fields, enum removal, or incompatible unit/identity behavior requires a new major schema version.
- Readers declare supported versions; unsupported versions fail explicitly.
- Golden fixtures cover canonicalization, serialization, migration, redaction, and old-reader behavior.
- Public plugin interfaces are not declared stable until a sample extension passes contract tests without internal dependencies.

## Local plugin and AI boundaries — Proposed (ADR-022, ADR-023)

The server worker owns RunAttempt leases; Agent is the selected executor, and Gateway relays guarded messages. Dispatch reservations, permits, and stop bounds are durable records separate from engine samples. See [database additions](../specs/database-schema.md#dispatch-persistence--proposed-adr-022); no agent EF/database dependency is introduced.

AIJob and ScenarioDraft are mutable revision-controlled workflow resources, not Runs or execution attempts. Publication links one reviewed revision to an immutable ScenarioVersion. AIExplanation is a separate artifact tied to a source report hash. Keys, raw prompts/contracts, and raw provider errors are not central records. Existing scenario/report schema shapes remain unchanged. API and protocol additions are draft interfaces until schemas, OpenAPI, golden fixtures, and generated clients exist; do not claim generation has already occurred.
