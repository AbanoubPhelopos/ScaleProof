# Finding (check result) contract v1

Status: Normative for finding semantics; repeated-query delivery moves to M0 optional telemetry under Proposed (ADR-022).
Audience: check authors, report consumers, frontend implementers
Machine-readable schema: [`schemas/check-result/v1.schema.json`](../../schemas/check-result/v1.schema.json)
Related: FR-023, FR-024, US-020, US-023, US-024, US-040, [ADR-003](../decisions/ADR-003-evidence-contract.md)

A **check** is a pure, versioned function: it takes a normalized evidence snapshot and returns findings. A check never sends traffic, runs queries, or reads outside its snapshot (US-040).

## Shape

```json
{
  "schemaVersion": "scaleproof.check-result/v1",
  "checkId": "scaleproof.repeated-query",
  "checkVersion": 1,
  "status": "warn",
  "severity": "medium",
  "confidence": "medium",
  "title": "Possible N+1: repeated query in GET /products/{id}",
  "summary": "Median trace executes the same query fingerprint 21 times (p95 38).",
  "evidence": [
    { "kind": "metric", "ref": "db_query_count", "request": "get-product", "aggregation": "p50", "value": 21, "unit": "count", "sampleCount": 412 },
    { "kind": "trace", "ref": "artifact:7f3c…#trace=4bf92f…", "note": "representative trace" },
    { "kind": "fingerprint", "value": "SELECT … FROM reviews WHERE product_id = ?", "count": 21 }
  ],
  "limitations": [
    "Repetition without ORM/framework evidence is reported as possible N+1, not proven.",
    "Only traces sampled by the collector were analysed (412 of 1,208 requests)."
  ],
  "recommendations": [ "Batch-load reviews for the product (e.g. a single query with IN, or eager loading)." ],
  "docs": "docs/checks/repeated-query.md"
}
```

## Field semantics

| Field | Values | Meaning |
|---|---|---|
| `status` | `pass`, `fail`, `warn`, `inconclusive`, `not_applicable`, `skipped`, `error` | **Outcome of the check.** `inconclusive` means evidence was insufficient. `not_applicable` means the check's preconditions don't apply (e.g. no DB spans because the app has no DB). `skipped` means disabled by config or policy. `error` means the evaluator itself failed, which is not a finding about the application. |
| `severity` | `info`, `low`, `medium`, `high`, `critical` | **Potential operational impact** if the finding is true. |
| `confidence` | `low`, `medium`, `high` | **Strength of the evidence.** It is not a probability. |
| `evidence[]` | typed references | Every status except `pass`, `not_applicable`, and `skipped` needs at least one evidence item, or a `limitations` entry explaining why evidence is unavailable (US-023). |
| `limitations[]` | strings | Mandatory for heuristic checks. |

Severity and confidence are independent. For example, "high severity, low confidence" is valid and means "potentially serious, weakly supported".

## Built-in checks

| ID | Version | Milestone | Requires | Description |
|---|---|---|---|---|
| `scaleproof.thresholds` | 1 | M0 | HTTP evidence | One finding per scenario threshold |
| `scaleproof.generator-validity` | 1 | M0 | generator samples | Surfaces quality-policy reasons as findings |
| `scaleproof.error-budget` | 1 | M0 | HTTP evidence | Status-class / timeout breakdown when `http_req_failed` > 0 |
| `scaleproof.repeated-query` | 1 | M0 (ADR-022) | OTel DB client spans | Possible N+1 by fingerprint repetition per server trace (US-020) |
| `scaleproof.idempotency-replay` | 1 | M3 | probe + fixture | Sequential/concurrent replay invariants (US-021/022) |

## Authoring a check (contributor contract)

1. Implement `ICheck` in `ScaleProof.Application/Checks/<Name>/`:
   ```csharp
   public interface ICheck
   {
       CheckDescriptor Descriptor { get; }  // id, version, required evidence kinds, config JSON Schema
       IReadOnlyList<Finding> Evaluate(EvidenceSnapshot evidence, JsonElement config);
   }
   ```
   It must be deterministic. No I/O, no clock (use `evidence.AnalyzedAt`), no randomness unless it is seeded from the run ID.
2. Add the config schema at `schemas/checks/<id>/v<version>.schema.json`.
3. Add golden fixtures in `tests/fixtures/checks/<id>/`. Each fixture is an evidence snapshot plus its expected findings, and must cover pass, fail, inconclusive (missing evidence), and not_applicable. Use Verify snapshot tests.
4. Write the user-facing doc page at `docs/checks/<id>.md`, covering what the check detects, its limitations, and how to fix the issue.
5. A changed meaning requires a new `checkVersion`. The old version remains selectable until it is deprecated per [release policy](../engineering/release-and-versioning.md).

## AI boundary — Proposed (ADR-023)

Built-in checks execute deterministically in the server Application layer over admitted evidence. AI output is not a CheckResult and cannot edit its status, severity, confidence, evidence, or recommendations. Display optional model hypotheses in a separately labeled explanation artifact linked to the report hash. Missing telemetry is not evidence that the backend has no database; distinguish unavailable evidence from an explicitly inapplicable check.

## .NET wire model

[`FindingV1`](../../src/ScaleProof.Contracts/Findings/FindingV1.cs) and
[`FindingJsonContext`](../../src/ScaleProof.Contracts/Findings/FindingJsonContext.cs)
provide the implemented check-result v1 wire model and serialization metadata.
Validate against the existing schema before deserialization; DTO reading does
not enforce the conditional evidence-or-limitation rule or evaluate a check.
Additive root/evidence properties are ignored, consistent with the schema; known
fields survive round trips. See [finding contract tests](../../tests/README.md#finding-v1-contracts-m0-p01-t04)
for coverage and reader limits. Heuristic identification and meaningful limitation
text remain evaluator responsibilities.
