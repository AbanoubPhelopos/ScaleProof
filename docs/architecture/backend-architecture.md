# Backend architecture

Status: Proposed (ADR-022, ADR-023); existing Accepted safety and evidence invariants remain binding.
Audience: backend implementers, coding agents
Related: [blueprint](implementation-blueprint.md) · [API reference](../specs/api-reference.md) · [database schema](../specs/database-schema.md) · [run lifecycle](../specs/run-lifecycle.md) · [k6 adapter](../specs/k6-adapter.md) · [configuration](../specs/configuration.md)

## Runtime boundary

`ScaleProof.Api` handles short requests, admission, project authorization, reads, and SSE. Its gateway module authenticates enrolled agents and relays durable messages. `ScaleProof.Worker` owns orchestration/leases, evidence verification, deterministic analysis, and report finalization. Neither process sends workload traffic in the initial release.

`ScaleProof.Agent`, distributed as `scaleproof`, owns command/service modes, local AI consent/calls, manifest enforcement, k6, normalization, redaction, and uploads. It references only Contracts and Execution; it has no server persistence or authoritative check engine. AI features are Agent modules, not additions to the k6 execution adapter. Shared DTO/schema validation lives at the Contracts/Execution boundary; server use cases revalidate publication/admission independently.

## ASP.NET Core conventions

- Use Minimal APIs grouped by feature under `/api/v1`.
- Give every operation a stable OpenAPI operation ID, tags, response declarations, authorization metadata, and body limits.
- Emit OpenAPI 3.1 at build time using `Microsoft.AspNetCore.OpenApi`; it drives frontend generation and contract tests.
- Use `System.Text.Json` web defaults, camelCase, declared string enums, and UTC ISO-8601 timestamps.
- Return Problem Details with stable `code`, safe detail, validation errors, and correlation ID.
- Propagate cancellation through database, file, process, and HTTP operations.
- Apply request limits at the API and stricter workload limits in the policy engine.

Feature folders contain endpoint, request/response mapping, validator, command/query, handler, and result. Handlers are explicit scoped services; do not introduce MediatR initially. Request validation uses the built-in .NET 10 Minimal API validation (`AddValidation()` with DataAnnotations) at the boundary plus explicit domain validation in handlers. Do not add FluentValidation without an ADR.

## Persistence

Use one `ScaleProofDbContext`, PostgreSQL migrations in Infrastructure, snake_case naming, UUID keys, `timestamptz`, explicit numeric precision, and JSONB only for versioned documents/evidence that do not need normalized querying.

- Keep API DTOs separate from persistence entities.
- Use projections and no-tracking read queries.
- Use optimistic concurrency tokens for mutable control records.
- Use conditional SQL for lease claim, heartbeat, and terminal transitions.
- Migrations are forward-only during normal development and include rollout/recovery notes when production-impacting.
- Seed deterministic local/sample data only.

## Worker loop

1. Claim an eligible `local_agent` run, reserve its agent slot, and create an attempt in one guarded transaction.
2. Persist a signed manifest dispatch; await the selected agent's validated accept. Commit a single start authorization under the current lease before relaying it.
3. Renew the server lease while agent execution heartbeats remain fresh. Publish execution permits no later than that lease or the signed execution deadline. During server analysis renew the lease without requiring the already stopped executor.
4. Observe cancellation, disconnect, revocation, engine expiry, and stale-worker fencing. Do not offer a replacement until the old executor's stop bound is satisfied.
5. Admit bounded progress and verified artifact references, never agent-provided verdicts. Persist `collecting` and `analyzing` only through the lease owner's conditional updates.
6. Run pure checks/thresholds/quality, render reports, and finalize attempt and run atomically under the lease.
7. Sweep expired dispatches/leases, job lifetimes, and retention. AI jobs have a separate lifecycle and no workload lease authority.

The [lifecycle](../specs/run-lifecycle.md) and [database](../specs/database-schema.md#lease-sql) specify guards. Poll with jitter and bounded concurrency; correctness never depends on in-memory notifications. Do not introduce a second queue technology.

## Shared execution boundary

- Render only validated intermediate definitions; never interpolate scenario text into a shell command.
- Use `ProcessStartInfo.ArgumentList`, explicit executable/version, sanitized environment, dedicated directory, redirected streams, timeout, output cap, and process-tree termination.
- Parse structured output incrementally and redact before storage.
- Record engine/adapter versions, safe command shape, exit status, termination reason, and generator diagnostics.

## Artifact storage

`IArtifactStore` provides stream-based immutable put/open/delete operations. Development uses a filesystem root outside web content. Production uses S3-compatible object storage. Keys are generated IDs, never user paths. Writes/uploads are content-hashed and idempotent. Authorized API endpoints or short-lived upload grants mediate access.

## Live progress

Expose authenticated SSE at `/events/v1/runs/{runId}`. Small events contain sequence, lifecycle, quality, bounded progress, and timestamp—never raw output or secrets. Persist events so API instances can replay from `Last-Event-ID`. If history expired, instruct the client to refetch. The UI falls back to capped polling.

## Identity modes

- **Local (loopback development, `ScaleProof:Mode=Local`):** API binds to loopback; CORS allows only the configured Vite origin; mutations use an unguessable local session plus antiforgery protection.
- **Hosted (M0 remote stage onward, `ScaleProof:Mode=Hosted`):** ASP.NET Core handles OIDC authorization-code flow with PKCE and secure `HttpOnly`, `SameSite=Lax`, `Secure` cookie. Browser storage never holds tokens. Roles are `Owner`, `Maintainer`, and `Viewer`, scoped by project. Agent identity uses separate mTLS credentials.

Application commands/queries enforce project authorization in addition to endpoint metadata. Deployed identity is required before any non-loopback availability; actor/project abstractions and audit fields exist from the first schema.

## Rate limiting and durable quotas

ASP.NET Core partitioned limiters protect each API instance using trusted identity/project/IP partitions and endpoint-specific token, window, concurrency, connection, and byte policies. Rejections return `429` plus `Retry-After` and stable Problem Details.

Per-instance counters are not the authority for shared workload safety. PostgreSQL atomically enforces project-wide queued/active runs, daily submissions, agent count, artifact bytes, mutation approvals, and target concurrency during admission. The agent enforces signed workload limits locally; workers independently guard dispatch and evidence admission.

## Reliability and observability

Use validated typed options, `IHttpClientFactory`, structured event IDs, OpenTelemetry, health checks, and centralized exception mapping. Retry only transient idempotent internal operations with bounded backoff. Never retry target mutations or policy rejection. API readiness requires PostgreSQL/artifact metadata access; worker readiness requires queue/artifact access; agent readiness additionally requires compatible k6 and a writable local workspace. AI configuration affects only the AI capability.

## AI scheduling — Proposed (ADR-023)

API jobs carry project/agent identity, task kind, input hash, prompt version, expiry, and safe status. The agent resolves context and endpoint locally, obtains disclosure approval, and calls the provider. The server stores reviewed redacted outputs/provenance, never keys or raw provider errors. Draft publication is a distinct revision-bound transaction; inference completion is not approval. See the [AI specification](../specs/ai-assistance.md) for lifecycle and resource contracts.
