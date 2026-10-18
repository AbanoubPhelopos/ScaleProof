# Events and agent protocol

Status: SSE is Normative. The agent protocol envelope is Proposed ([ADR-018](../decisions/ADR-018-agent-protocol-messages.md)), and manifest signing is Proposed ([ADR-014](../decisions/ADR-014-jws-signed-manifests.md)).
Audience: backend, frontend, and agent implementers
Related: FR-016, FR-042, FR-046, [ADR-011](../decisions/ADR-011-sse-live-progress.md), [ADR-012](../decisions/ADR-012-local-agent-tunnel.md), [local agent architecture](../architecture/local-agent-and-tunnel.md), [run lifecycle](run-lifecycle.md)

## SSE run events

`GET /events/v1/runs/{runId}` with `Accept: text/event-stream`. The caller must be authorized for the run's project.

- **Frames:**
  - `id: <seq>` is a per-run monotonically increasing integer.
  - `event: <type>`.
  - `data:` holds a single-line JSON payload of ≤ 4 KiB.
- **Resume:** the browser sends `Last-Event-ID`. The server replays persisted events with `seq > Last-Event-ID`. If those have expired (retention `Sse:EventRetention`, default 1 h after terminal), it sends a single `stream.reset` and the client must refetch `GET /runs/{id}`.
- **Heartbeat:** a comment line `: hb` every 15 s. The server closes the stream 5 s after a terminal event or at `Sse:MaxLifetime` (default 30 min). The client reconnects if the run is not terminal.
- **Rate:** at most 2 `run.progress` events/s per attempt. Lifecycle events are never dropped.

| Event | Payload | Client behavior |
|---|---|---|
| `run.lifecycle` | `{ runId, attemptOrdinal, lifecycle, previous, at }` | Update cache |
| `run.progress` | `{ runId, attemptOrdinal, window, elapsedMs, requests, failedRatio, achievedRate, p95Ms?, vus, at }` (bounded, rounded) | Update live tiles and chart. Never treated as final |
| `run.quality` | `{ runId, qualityStatus, reasons[] }` | Update |
| `run.terminal` | `{ runId, lifecycle, qualityStatus, outcome, terminationReason, at }` | Close the stream, **refetch durable state**, and ignore any later stale event |
| `stream.reset` | `{ reason: "history_expired" }` | Refetch and reconnect without `Last-Event-ID` |

Fallback: after 3 failed reconnects in 60 s, the UI polls `GET /runs/{id}` every 5 s (capped at 10 minutes) and shows "Live updates degraded".

## Agent control protocol

*Proposed (ADR-022, ADR-018), M0.* The transport is WebSocket `wss://…/agent/v1/connect`, subprotocol `scaleproof.agent.v1`, with mTLS client certificate auth. Frames are UTF-8 JSON with a maximum of 64 KiB per frame.

### Envelope

```jsonc
{
  "v": 1,
  "type": "manifest.offer",
  "id": "01J9…",            // message ID (UUIDv7), unique per sender
  "seq": 42,                // sender's monotonically increasing sequence within session
  "ack": 17,                // highest peer seq this sender has processed
  "sessionId": "…",
  "agentId": "…",
  "sentAt": "2026-10-17T10:00:00Z",
  "expiresAt": "2026-10-17T10:00:30Z",
  "correlationId": "…",
  "payload": { }
}
```

- **Ordering and resume:** each side keeps unacknowledged messages, up to 256 messages or 1 MiB. On reconnect, `hello` carries the last `ack`, and the peer retransmits anything after it. A duplicate `id` is idempotent: it is processed once and acknowledged again.
- **Rejection:** an expired message, or one carrying an unknown `v`, is rejected with `error{code}` and is not processed.

### Message types

| Type | Direction | Payload (essential) | Notes |
|---|---|---|---|
| `hello` | A→G | `agentVersion`, `protocolVersions[]`, `lastAck`, `os`, `arch`, `cpuCount` | First frame |
| `hello.ok` | G→A | `sessionId`, `serverTime`, `jwks`, `minAgentVersion` | Or `hello.upgrade_required{downloadUrl, minVersion}` and then the server closes |
| `heartbeat` | both | `{}` | Every 15 s; 3 missed means the session is dead |
| `capabilities` | A→G | `engines[{id, version}]`, `targetRuleIds[]`, `maxConcurrentRuns`, `telemetryModes[]`, `aiAvailable`, `aiProfile`, `localContextIds[]` | On connect and on change |
| `manifest.offer` | G→A | `manifest` (JWS compact) | |
| `manifest.accept` | A→G | `attemptId`, `jti` | Agent has verified everything listed below |
| `manifest.reject` | A→G | `attemptId`, `reason` ∈ `signature_invalid`, `expired`, `replayed`, `wrong_agent`, `rule_mismatch`, `limits_exceed_local`, `engine_unavailable`, `busy`, `version_unsupported`, `clock_uncertain` | Run goes to `failed` with `agent_rejected:<reason>` |
| `attempt.start` | G→A | `attemptId`, `dispatchId`, `executionPermitUntil`, `executionDeadline` | Committed worker start authorization; agent journals once before launch (Proposed ADR-022) |
| `attempt.permit` | G→A | `attemptId`, `dispatchId`, `executionPermitUntil` | Current worker lease required; no later than signed deadline (Proposed ADR-022) |
| `attempt.heartbeat` | A→G | `attemptId`, `dispatchId`, `engineRunning`, `at` | Execution liveness independent of progress (Proposed ADR-022) |
| `attempt.started` | A→G | `attemptId`, `engineVersion`, `startedAt` | Worker maps to lifecycle `running` only under matching lease/dispatch/permit |
| `attempt.progress` | A→G | Same as SSE `run.progress` | ≤ 2/s |
| `cancel.request` | G→A | `attemptId`, `reason` | |
| `cancel.ack` | A→G | `attemptId`, `stoppedAt` | |
| `upload.grant.request` | A→G | `attemptId`, `kind`, `bytes`, `sha256` | |
| `upload.grant` | G→A | `artifactId`, `url`, `expiresAt`, `maxBytes` | Short-lived, single object |
| `attempt.execution_finished` | A→G | `attemptId`, `dispatchId`, `engineStoppedAt`, `engineExit`, `artifacts[{artifactId, sha256}]`, `summary` | Proposed ADR-022: execution/evidence input only, never an outcome or authoritative terminal event |
| `execution.ack` | G→A | `attemptId`, `dispatchId` | Evidence committed; local cleanup follows retention policy; server analysis may still be running |
| `error` | both | `code`, `refId` | Protocol-level errors only |

### Execution manifest

Proposed (ADR-014): the manifest is a JWS compact token, ES256 ([ADR-014](../decisions/ADR-014-jws-signed-manifests.md)).

- **Header:** `typ: scaleproof-manifest+jwt`, `alg: ES256`, `kid`.
- **Registered claims:** `iss`, `aud` (= agentId), `sub` (= attemptId), `iat`, `nbf`, `exp` (≤ 5 min after `iat`), `jti` (nonce), `ver: 1`.
- **Private claims:**

```json
{
  "runId": "…", "projectId": "…", "attemptOrdinal": 1, "dispatchId": "…",
  "executionDeadline": "2026-10-17T10:15:20Z",
  "hashes": { "scenario": "sha256:…", "target": "sha256:…", "policy": "sha256:…" },
  "targetRule": { "id": "…", "version": 2, "scheme": "http", "host": "127.0.0.1", "port": 5080, "basePath": "/api" },
  "allowed": { "methods": ["GET"], "pathPatterns": ["/api/products", "/api/products/*"] },
  "limits": { "maxRate": 100, "maxVus": 100, "maxRequests": 60000, "maxDurationMs": 600000,
              "maxRequestBytes": 65536, "maxResponseBytes": 1048576, "maxArtifactBytes": 52428800, "requestTimeoutMs": 10000 },
  "mutation": { "classification": false, "approvalRef": null },
  "engine": { "id": "k6", "versionRange": ">=1.0.0 <2.0.0", "adapterVersion": "1" },
  "plan": { "…": "canonical execution plan; the agent re-derives and re-checks it against targetRule and limits" },
  "offlineGraceMs": 30000
}
```

Agent verification order, which stops at the first failure:

1. Signature and `alg`/`kid`
2. `aud`
3. `exp`/`nbf` for acceptance/start, with conservative server-time uncertainty bounded by configuration; separate signed `executionDeadline` bounds process stop
4. `jti` not seen before
5. `projectId` matches enrollment
6. `targetRule` exists locally, is identical, and is not revoked
7. The plan's destinations, methods, and paths lie within the rule and `allowed`
8. Limits ≤ local ceiling
9. Engine available in range
10. Journal dispatch/nonce durably; accept does not start traffic. Await a matching committed `attempt.start` permit under the [lifecycle fencing rules](run-lifecycle.md#agent-dispatch-and-stop-fencing--proposed-adr-022).

## Optional diagnostic tunnel

*Milestone M3.* The tunnel is specified behaviorally in [local agent architecture](../architecture/local-agent-and-tunnel.md#optional-diagnostic-http-tunnel). Its wire format will be added here before implementation begins.

## Dispatch authority — Proposed (ADR-022)

All attempt messages additionally carry `dispatchId`; gateway checks enrolled project/agent, current attempt/ordinal, valid worker lease, allowed state, and persisted message identity. Keep worker lease tokens server-side. Upload URLs are restricted to locally approved artifact-service origins. Session `heartbeat` proves connectivity only; `attempt.heartbeat` proves execution liveness. Gateway persists ordered input for the owning worker; it cannot renew leases or issue authority independently. Ack sequence numbers alone never authorize execution.

The changed messages are a draft revision of the unimplemented Proposed ADR-018 protocol, not a claim that deployed v1 clients already support them. Before release, publish agent-protocol JSON Schemas and golden compatibility fixtures covering start/permit/finish fencing; reject agents that lack the required capability. `exp` is the launch window; signed `executionDeadline` is at most issuance + manifest TTL + maximum workload duration + process-stop budget. Local workload duration and permit expiry still impose earlier stops.

## AI jobs — Proposed (ADR-023)

AI capability reports only availability, supported `chat-completions/1` profile, and locally registered opaque context IDs. It never advertises a key, local path, prompt, or environment value. Bind every job to project, enrolled agent, kind, expiry, input hash once consented, and prompt version. No AI message contains workload start authority.

| Type | Direction | Payload and behavior |
|---|---|---|
| `ai.job.offer` | G→A | `jobId`, `kind` (`scenario_draft` or `explanation`), `contextId` or pinned report/artifact references, `expiresAt`, `requestedBy`; resolve only registered local context or authorized evidence |
| `ai.job.accept` | A→G | `jobId`, approved `inputHash`, `promptVersion`; emitted only after local preview/consent; server records `queued` |
| `ai.job.start` | G→A | `jobId`, matching `inputHash`; server records `running`; agent journals request-once before inference |
| `ai.job.result` | A→G | `jobId`, input/output hashes, schema version, approved redacted result upload reference, safe provenance; requires local output review and server schema/hash checks |
| `ai.job.failed` | A→G | `jobId`, bounded safe catalog code; never raw provider error/body |
| `ai.job.cancel` | G→A | `jobId`; cancel locally; late results cannot publish |
| `ai.job.ack` | G→A | `jobId`, durable state; duplicate same-hash result returns the same resource |

Store request-once fences locally. Reconnection does not repeat inference; an interrupted running job fails and requires a new explicit job. The gateway may send cancellation immediately; state changes are durable compare-and-set operations on the AI job, independent of worker workload leases. The [AI spec](ai-assistance.md) owns consent, review, and expiry behavior. Browser job views use bounded polling of the job resource; run SSE does not wait for AI completion.
