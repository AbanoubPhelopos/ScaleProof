# HTTP API reference (v1)

Status: Normative for existing HTTP conventions; plugin-first scheduling and AI additions are Proposed (ADR-022, ADR-023). After that, the build-time `openapi.json` is authoritative and this page summarizes it.
Audience: backend/frontend implementers, CLI and CI integrators
Related: [backend architecture](../architecture/backend-architecture.md#aspnet-core-conventions), [error codes](error-codes.md), [events](events-and-agent-protocol.md), [ADR-010](../decisions/ADR-010-openapi-generated-client.md)

## Conventions

| Topic | Rule |
|---|---|
| Base paths | `/api/v1` (JSON REST), `/events/v1` (SSE), `/agent/v1` (agent gateway, M0 under Proposed ADR-022), `/health/*` |
| Format | `application/json`, `System.Text.Json` web defaults: camelCase, string enums (snake_case values), UTC RFC 3339 timestamps |
| IDs | UUIDv7 strings (time-ordered; generated server-side unless stated) |
| Errors | RFC 9457 Problem Details with extension members `code`, `correlationId`, and `errors` (validation: `{ pointer, code, message }[]`). Catalog: [error codes](error-codes.md) |
| Status codes | `200` read, `201 Created` + `Location` for configuration resources, `202 Accepted` for asynchronous commands, `400` validation, `401`/`403`, `404`, `409` version/idempotency/state conflict, `413` body too large, `422` policy denial, `429` rate limited (+ `Retry-After`) |
| Pagination | `?limit=` (default 50, max 200) and `?cursor=` (opaque). Order is `createdAt desc, id desc`. Response: `{ "items": [], "nextCursor": "…" \| null }` |
| Concurrency | Mutable control records return an `ETag`. Updates require `If-Match`. A mismatch returns `409 VERSION_CONFLICT` |
| Idempotency | `POST /runs` and `POST /comparisons` require an `Idempotency-Key` header (1–128 chars). The key is scoped to (project, actor, route) and retained 24 h. The same key with the same body hash replays the original response. The same key with a different body gives `409 IDEMPOTENCY_KEY_REUSED` |
| Auth | M0: loopback plus a local session cookie and antiforgery header `X-ScaleProof-CSRF` on mutations. Remote M0 stage (Proposed ADR-022): OIDC cookie session and antiforgery; local CLI uses its scoped enrolled mTLS identity for project commands, never user administration |
| Correlation | `X-Correlation-Id` is accepted (validated `^[A-Za-z0-9-]{8,64}$`) or generated, and echoed on every response |
| Body limits | 256 KiB default; 1 MiB for dataset upload; larger artifacts use upload grants |

## Endpoints

| Method | Route | Operation ID | Milestone | Success | Notes |
|---|---|---|---|---|---|
| GET | `/health/live` | — | M0 | 200 | No dependencies |
| GET | `/health/ready` | — | M0 | 200/503 | DB and artifact store reachable; no details in body |
| GET | `/api/v1/projects` | `listProjects` | M0 | 200 | |
| POST | `/api/v1/projects` | `createProject` | M0 | 201 | |
| GET | `/api/v1/projects/{projectId}` | `getProject` | M0 | 200 | |
| GET/POST | `/api/v1/projects/{projectId}/targets` | `listTargets` / `createTargetVersion` | M0 | 200/201 | POST creates a new immutable version |
| GET/POST | `/api/v1/projects/{projectId}/scenarios` | `listScenarios` / `createScenarioVersion` | M0 | 200/201 | Body: YAML (`application/yaml`) or JSON |
| GET | `/api/v1/projects/{projectId}/scenarios/{name}/versions/{version}` | `getScenarioVersion` | M0 | 200 | Includes canonical form and hash |
| GET/POST | `/api/v1/projects/{projectId}/datasets` | `listDatasets` / `createDatasetVersion` | M0 | 200/201 | ≤ 1 MiB |
| POST | `/api/v1/validations` | `validate` | M0 | 200 | Always 200 with `{ valid, errors[], effectivePolicy, scenarioHash }`. **Sends no traffic** |
| POST | `/api/v1/runs` | `submitRun` | M0 | 202 | `Idempotency-Key` required. Returns run summary + `Location` |
| GET | `/api/v1/projects/{projectId}/runs` | `listRuns` | M0 | 200 | Filters: `scenario`, `lifecycle`, `qualityStatus`, `from`, `to` |
| GET | `/api/v1/runs/{runId}` | `getRun` | M0 | 200 | Lifecycle, quality, outcome, progress summary |
| POST | `/api/v1/runs/{runId}/cancellation` | `cancelRun` | M0 | 202 | Idempotent. Optional body `{ "note": string ≤ 200 }`, recorded in audit only. Response: run summary (`runId`, `lifecycle`, `qualityStatus`, `outcome`, `terminationReason`). Role: Maintainer/Owner or enrolled cancellation scope (Proposed ADR-022) |
| GET | `/api/v1/runs/{runId}/report` | `getReport` | M0 | 200 / 409 `REPORT_NOT_READY` | `Accept: application/json` or `text/markdown` |
| GET | `/api/v1/runs/{runId}/artifacts` | `listArtifacts` | M0 | 200 | |
| GET | `/api/v1/runs/{runId}/artifacts/{artifactId}` | `downloadArtifact` | M0 | 200 (stream) | `Content-Disposition: attachment`; never rendered inline |
| GET | `/api/v1/runs/{runId}/metrics` | `getRunMetrics` | M0 | 200 | `?window=&request=&interval=` for chart series |
| POST | `/api/v1/projects/{projectId}/baselines` | `setBaseline` | M1 | 201 | Only `valid` runs |
| POST | `/api/v1/comparisons` | `createComparison` | M1 | 202/200 | `Idempotency-Key` required |
| GET | `/api/v1/comparisons/{comparisonId}` | `getComparison` | M1 | 200 | |
| GET | `/api/v1/checks` | `listChecks` | M0 | 200 | IDs, versions, config schemas, required evidence |
| GET | `/events/v1/runs/{runId}` | — | M0 | 200 (SSE) | See [SSE events](events-and-agent-protocol.md#sse-run-events) |
| POST | `/api/v1/projects/{projectId}/agent-enrollments` | `createAgentEnrollment` | M0 (ADR-022) | 201 | Returns a one-time code once |
| GET | `/api/v1/projects/{projectId}/agents` | `listAgents` | M0 (ADR-022) | 200 | |
| POST | `/api/v1/agents/{agentId}/revocation` | `revokeAgent` | M0 (ADR-022) | 202 | |
| GET/POST | `/api/v1/agents/{agentId}/target-rules` | `listTargetRules` / `createTargetRuleVersion` | M0 (ADR-022) | 200/201 | |
| GET | `/api/v1/projects/{projectId}/quotas` | `getQuotas` | M0 (ADR-022) | 200 | |
| GET | `/agent/v1/connect` | — | M0 (ADR-022) | 101 | WebSocket upgrade; see [agent protocol](events-and-agent-protocol.md#agent-control-protocol) |

## Run submission

```http
POST /api/v1/runs
Idempotency-Key: 6f1c0a8e-ci-1234
Content-Type: application/json

{
  "projectId": "0192…",
  "scenario": { "name": "catalog-read-baseline", "version": 3 },     // or "inline": { …scenario v1… }
  "target":   { "profile": "sample-api", "version": 2 },              // optional; defaults to latest version
  "executionLocation": "local_agent",
  "agentId": "0192…",                                                  // selected online enrolled agent (Proposed ADR-022)
  "build": { "label": "feature/x", "commit": "abc123" },               // optional, informational
  "repetitions": 1                                                    // M1: 1–10
}
```

```http
HTTP/1.1 202 Accepted
Location: /api/v1/runs/0192…

{ "runId": "0192…", "lifecycle": "queued", "qualityStatus": null, "outcome": null,
  "scenarioHash": "sha256:…", "effectivePolicy": { … }, "eventsUrl": "/events/v1/runs/0192…" }
```

An inline scenario is versioned implicitly (it creates a scenario version), so every run references an immutable snapshot (FR-001).

## Plugin-first admission — Proposed (ADR-022)

Only `local_agent` is accepted initially; `agentId` is required and must belong to the project, be online, compatible, not revoked, and have an approved matching local rule. Reserved other locations return `EXECUTION_LOCATION_UNSUPPORTED`. Admission cannot reserve more active work than the project/agent cap; claim rechecks capacity. No agent switch or server fallback occurs. Both CLI and browser submit the same immutable scenario version; generated drafts are not run inputs until published and revalidated.

CLI agent certificates grant explicit project command scopes (`drafts:write`, `scenarios:publish`, `runs:submit`, `runs:cancel`, `reports:read`) assigned at enrollment, not user/session administration. Maintainer/Owner grants these scopes and may revoke them. Every handler enforces scopes/roles and project membership. A browser's Viewer role cannot publish or launch. Audit records both requesting user, when present, and executing agent.

## AI and draft resources — Proposed (ADR-023)

All routes below are project-scoped. Apply the existing command rate limiter, byte limits, pagination, ETags, safe errors, and foreign-resource 404 behavior. Commands require project Maintainer/Owner or the narrowly corresponding enrolled command scope. Reads require project access. `POST` commands require `Idempotency-Key` with the existing retention/body-hash rules. No route accepts provider credentials or local filesystem paths.

| Method | Route below `/api/v1/projects/{projectId}` | Operation ID | Success | Purpose |
|---|---|---|---|---|
| POST | `/ai-jobs` | `createAiJob` | 202 | Request `kind`, `agentId`, and opaque local `contextId` for generation, or `runId`/source report artifact for explanation; initial state `awaiting_local_approval` |
| GET | `/ai-jobs/{jobId}` | `getAiJob` | 200 | Safe state, timestamps, input hash once approved, result resource reference or safe failure code |
| POST | `/ai-jobs/{jobId}/cancellation` | `cancelAiJob` | 202 | Idempotent cancellation; cannot alter the run |
| GET | `/scenario-drafts` | `listScenarioDrafts` | 200 | Paginated safe drafts |
| POST | `/scenario-drafts` | `createScenarioDraft` | 201 | Upload locally reviewed redacted draft/result with provenance, optional `jobId`; server revalidates |
| GET | `/scenario-drafts/{draftId}` | `getScenarioDraft` | 200 | Revision, candidate, questions, assumptions, validation issues, review state and ETag |
| PUT | `/scenario-drafts/{draftId}` | `reviseScenarioDraft` | 200 | `If-Match` required; create new revision, invalidate prior review |
| POST | `/scenario-drafts/{draftId}/publication` | `publishScenarioDraft` | 201 | `If-Match` and reviewed revision/operation/SLO confirmations required; atomically create one immutable scenario version; never launch |
| POST | `/runs/{runId}/ai-explanations` | `createAiExplanation` | 201 | Admit locally reviewed `scaleproof.ai-explanation/v1` output tied to job/source report/input hash; return authorized artifact reference |
| GET | `/runs/{runId}/ai-explanations` | `listAiExplanations` | 200 | Paginated independent explanation versions/status; report remains separately available |

`createAiJob` returns `{ jobId, state, expiresAt }` and `Location`. A context ID is registered by the local agent and maps to a previously selected contract/goals; remote callers cannot create file mappings. Explanation jobs require a terminal run and a pinned report artifact. An AI job becoming completed means the reviewed result was admitted; it does not publish a scenario version. CLI-created local inference may remain off-server until the user uploads the reviewed output. Draft/explanation uploads include endpoint identity without credentials, model, prompt version, input/output hashes, and local review actor/time; these are provenance, not trusted attestations.

Draft publication requires a current `ready_for_review` revision, no unresolved questions, confirmed SLOs and explicitly confirmed non-mutating operations, complete fixture/auth references, and successful server validation. Its transaction writes scenario version, provenance, publication result/idempotency record, and audit event. Concurrent publication requests for one revision resolve to the same scenario version. Any edit after publication is a new draft revision; completed scenario/run records remain immutable.
