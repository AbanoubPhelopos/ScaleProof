# Report contract v1

Status: Normative for report shape; example execution location follows Proposed (ADR-022), and optional AI artifacts are Proposed (ADR-023).
Audience: report renderers, CI integrators, frontend implementers
Machine-readable schema: [`schemas/report/v1.schema.json`](../../schemas/report/v1.schema.json)
Related: FR-030, FR-031, [ADR-005](../decisions/ADR-005-versioned-reports.md), [metrics](metrics-and-statistics.md), [finding](finding-v1.md), [comparison](comparison.md)

The JSON report is the automation contract. Markdown (and later HTML) is a **rendering** of the JSON and never contains facts that the JSON lacks. A report can be regenerated from the retained normalized evidence (FR-030). Each regeneration is a new report artifact version, and the run is not modified.

## Top-level structure

```json
{
  "schemaVersion": "scaleproof.report/v1",
  "reportVersion": 1,
  "generatedAt": "2026-10-17T10:02:41Z",
  "tool": {"name": "scaleproof", "version": "0.1.0"},
  "run": {"runId": "01990000-0000-7000-8000-000000000001", "attemptId": "01990000-0000-7000-8000-000000000002", "attemptOrdinal": 1, "projectId": "01990000-0000-7000-8000-000000000003", "lifecycle": "completed", "qualityStatus": "valid", "outcome": "pass", "terminationReason": "completed", "executionLocation": "local_agent", "submittedAt": "2026-10-17T09:59:59Z", "startedAt": "2026-10-17T10:00:00Z", "finishedAt": "2026-10-17T10:02:40Z"},
  "identity": {"scenario": {"name": "catalog-read-baseline", "version": 3, "hash": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}, "target": {"profile": "sample-api", "version": 2, "hash": "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"}, "policy": {"version": 5, "hash": "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"}, "engine": {"id": "k6", "version": "1.0.0", "adapterVersion": "1", "templateHash": "sha256:dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"}, "environment": {"fingerprint": "sha256:eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee", "agentId": "01990000-0000-7000-8000-000000000004"}},
  "workload": {"profile": "arrival_rate", "windows": [{"name": "warmup", "start": "2026-10-17T10:00:00Z", "durationMs": 30000}, {"name": "measurement", "start": "2026-10-17T10:00:30Z", "durationMs": 120000}, {"name": "rampdown", "start": "2026-10-17T10:02:30Z", "durationMs": 10000}], "offeredRate": 50, "achievedRate": 49.7, "droppedIterations": 0},
  "metrics": [{"metric": "http_req_duration", "aggregation": "p95", "unit": "ms", "value": 241.9, "sampleCount": 5964, "percentileStatus": "stable", "window": "measurement", "source": "k6", "definitionVersion": 1, "unavailableReason": null}],
  "perRequest": [],
  "thresholds": [{"metric": "http_req_duration", "aggregation": "p95", "operator": "<", "value": 300, "unit": "ms", "observed": 241.9, "status": "pass"}],
  "quality": {"status": "valid", "policyVersion": "quality/1", "reasons": []},
  "findings": [],
  "comparison": null,
  "limitations": ["Synthetic documentation example; identities/hashes are illustrative, not a reproducible run.", "Server-side latency unavailable: telemetry not configured."],
  "redaction": {"policyVersion": "redaction/1", "redactedFields": 0, "partial": false},
  "artifacts": []
}
```

### MetricValue

```json
{ "metric": "http_req_duration", "aggregation": "p95", "unit": "ms", "value": 241.9,
  "sampleCount": 5964, "percentileStatus": "stable", "window": "measurement",
  "source": "k6", "definitionVersion": 1, "unavailableReason": null }
```

`value: null` requires an `unavailableReason` ∈ `insufficient_samples`, `not_collected`, `telemetry_unavailable`, `redacted`, `engine_unsupported`.

## Rules

- Timestamps are UTC RFC 3339. Durations are integer milliseconds. Sizes are integer bytes.
- Enums are lowercase snake_case strings. Readers must reject unknown `schemaVersion` majors and **ignore** unknown properties within v1 (additive evolution).
- Client latency (`http_req_*`) and server latency (`server_span_duration`) are never merged.
- `diagnostic_tunnel` reports must include the limitation "Not valid for latency or capacity conclusions."

## Markdown rendering

Section order is fixed so diffs between reports are meaningful:

1. Verdict line: lifecycle · quality · outcome, plus the termination reason when not completed.
2. Identity table: scenario, target, engine, build, environment.
3. Compatibility, when comparing.
4. Thresholds.
5. Key metrics table with sample counts.
6. Findings: severity, confidence, evidence, limitations.
7. Quality reasons and limitations.
8. Artifacts with hashes.

Charts are not embedded. Every table includes units.

## AI explanation artifacts — Proposed (ADR-023)

`scaleproof.report/v1` stays deterministic and does not embed model text or model-dependent verdicts. Store `scaleproof.ai-explanation/v1` as a separate run/attempt-linked artifact with source report hash, local input hash, model/prompt/redaction versions, evidence references, limitations, hypotheses, and output review provenance as specified by [AI assistance](ai-assistance.md). The explanation listing endpoint may return additional versions; downloading the deterministic report never waits for them. Reports generated before this feature remain valid without it.

The dashboard labels AI explanation availability independently from lifecycle/quality/outcome. An inference error is not a failed run. HTTP-only reports cannot claim database/runtime root causes. Comparison and future re-analysis select exact report hashes; an explanation over an older hash remains explicitly attached to that version.
