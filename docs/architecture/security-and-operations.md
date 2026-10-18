# Security and operations

Status: Normative for existing safety controls; plugin-first restrictions and AI boundary are Proposed (ADR-022, ADR-023).
Audience: security reviewers, operators, implementers
Related: [threat model](threat-model.md) · [configuration (limits)](../specs/configuration.md) · [deployment](deployment-architecture.md) · [error codes](../specs/error-codes.md)

## Trust boundaries

Untrusted inputs include scenarios, URLs, headers/data references, engine output, telemetry, local OpenAPI descriptions, AI output, and agent messages. Protected assets include user and agent credentials, signing keys, target availability, developer localhost/private networks, control-plane state, artifacts, and tenant/project boundaries.

The STRIDE analysis with per-threat tests is in [threat model](threat-model.md). Primary threats are abuse as a denial-of-service tool, SSRF/DNS rebinding, redirect escape, secret/PII leakage, arbitrary scenario/plugin execution, cross-project access, resource exhaustion, duplicate mutation execution, and stale retained artifacts.

## Default safety policy

- Local mode permits only explicitly configured local targets and binds control APIs to loopback while retaining local-session authentication and antiforgery.
- Proposed ADR-022: server-worker workload execution is unsupported initially, including when target profiles exist; it needs a later explicit implementation/design gate. Developer localhost uses the separate enrolled local-agent policy and never makes the control plane treat loopback as its own destination.
- Every hostname resolution and redirect destination is revalidated. Remote mode denies loopback, private, link-local, metadata, and control-plane ranges unless explicitly allow-listed for the intended network.
- Rate, concurrency, duration, total requests, timeouts, bytes, response capture, artifacts, queued jobs, and active workers are server/agent enforced.
- Proposed ADR-022: initial admission rejects write/mutation workloads. Future mutation/replay checks require isolated targets, deterministic fixtures, explicit probes/assertions, bounded observation, reset behavior, and an approval policy.
- Scenarios cannot execute arbitrary shell commands or unrestricted SQL. Later extensibility must define isolation and permissions first.
- Production stress is not supported in M0–M2. Future support requires named ownership/approval, window, endpoints, caps, aborts, monitoring, and authorization evidence.
- Localhost execution requires an enrolled outbound-only agent, signed expiring manifest, user-approved local rule, replay protection, and independent agent-side enforcement.
- The deferred M3 relay tunnel must be scoped to one run/origin and cannot be used as a generic proxy or performance evidence source.

## Layered abuse and rate controls

Controls are cumulative:

1. CDN/DDoS/WAF and ingress enforce TLS, hosts, connections, bodies, and coarse IP limits.
2. ASP.NET Core partitioned rate limiting protects endpoints by trusted actor/project/IP and returns `429` with `Retry-After`.
3. PostgreSQL admission enforces global project quotas for queued/active/daily runs, agents, artifacts, mutations, and workload budgets.
4. Queue, worker, target, and agent concurrency are bounded with back-pressure.
5. Worker revalidates dispatch/admission; agent enforces manifest rate, duration, VU, request, byte, timeout, and artifact ceilings.

Never derive a limiter partition from arbitrary unbounded user input. Trust forwarded client identity only from explicitly configured proxies. Health, SSE, enrollment, artifact, and agent-gateway routes have separate policies rather than blanket exemption.

## Secrets, privacy, and artifacts

Use short-lived credentials and named secret providers. Agent private keys remain local; server signing keys use the platform key/secret manager and rotate. Redact authorization headers, cookies, API keys, seeded test secrets, and known sensitive fields from logs, snapshots, traces, reports, commands, and exceptions.

Capture metadata only by default. Explicit field/header allow-lists, byte caps, retention, and a redaction-policy version are required for content capture. Large artifacts live outside relational run rows and are referenced by authorized metadata plus hashes.

Retention distinguishes run data, artifacts, and audit events. Expiry cannot delete active/comparison artifacts. Proposed ADR-022: [configuration](../specs/configuration.md#agent-fencing-and-capture-options--proposed-adr-022) owns retention/deletion defaults and stop-before-delete ordering. Document operator backup expiry/recovery guarantees before remote availability; do not promise deletion from unexpired backups.

## Abort and failure behavior

Automatic aborts include excessive error rate/consecutive failures, target timeouts, resource or request caps, generator saturation policy, optional target health failure, and lease/cancellation events. Stop new work, bound in-flight completion, terminate the engine safely, preserve partial evidence, and record the reason.

Transient internal operations may use bounded exponential backoff with jitter. Never blindly replay a started mutation attempt. Artifact upload is retryable only when stable IDs make the write idempotent.

## Observability

Structured logs include run, attempt, project (when safe), agent, component, transition, and correlation IDs. Aggregate metrics avoid run IDs, full URLs, trace IDs, commit hashes, and user tags as labels.

Minimum platform metrics cover submissions, starts/completions by bounded status, run/queue duration, active workers, lease expirations, cancellations, artifact bytes, check duration by registered ID, policy rejections by bounded reason, and telemetry failures.

Trace control-plane operations from submission through rendering. Run IDs may correlate traces, but secrets and arbitrary user data never become telemetry attributes.

## Health and runbooks

- Liveness proves the process is responsive.
- API readiness proves required state dependencies permit service.
- Worker readiness proves it can claim work and reach required state/artifact dependencies.
- Agent Gateway readiness proves it can authenticate sessions and reach durable sequence/state dependencies.

Runbooks are required for stuck runs, no worker claims, PostgreSQL failure, artifact failure, missing telemetry, generator saturation, incomplete cancellation, quota/storage pressure, unexpected policy rejection, agent disconnect storms, credential/signing-key compromise, manifest rejection spikes, and tunnel abuse.

## Verification checklist

- Target parsing, DNS changes, redirects, path normalization, and private/metadata range policy have adversarial tests.
- Authorization tests cover every project-owned record and artifact in hosted mode.
- Redaction tests seed recognizable secrets through inputs, exceptions, telemetry, and engine output.
- Cancellation, lease expiry, duplicate dispatch, mutation reconciliation, and retention races have integration tests.
- Enrollment expiry/single-use, mTLS rotation/revocation, sequence replay, signed-manifest tampering, reconnect, tunnel path escape, quota races, and layered rate-limit behavior have adversarial/integration tests.
- Dependency/action versions are pinned according to release policy; releases include vulnerability review and, when feasible, SBOM and signed artifacts.

## Local AI operations — Proposed (ADR-023)

Only locally configured endpoints receive sanitized model input after hash-bound disclosure consent. Dashboard commands cannot approve new local content, file access, endpoint changes, or secret disclosure. Keep AI keys out of snapshots, child environments, logs, provider errors, and browser fields. Treat imported descriptions as data, reject external refs, and validate/redact output before any persistence. Render explanations as inert plain text.

Inference has independent job state, request/byte/token/time/concurrency/daily budgets, and no automatic retries or repair loops. AI failures never change run/report results. Alert on safe bounded failure categories; never log raw prompts, output, error bodies, or authorization headers. Runbooks cover missing credentials, unsupported providers, local approval waiting/expiry, stuck/cancelled jobs, and sanitized-output validation failure. See [AI assistance](../specs/ai-assistance.md) for authoritative behavior.

Worker/agent loss runbooks must retain agent reservations until a matching stopped acknowledgment or persisted stop bound. Do not manually clear leases and relaunch traffic without reconciling outstanding permits/processes. Compare server time uncertainty and signed execution deadline when diagnosing permit rejection.
