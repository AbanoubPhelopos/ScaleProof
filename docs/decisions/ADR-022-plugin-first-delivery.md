# ADR-022: Local plugin as the primary execution surface

Status: Proposed
Date: 2026-10-18
Deciders: maintainers
Related: FR-016, FR-031, FR-046–FR-047, NFR-003; proposed partial supersession of [ADR-002](ADR-002-modular-monolith-and-worker.md) and [ADR-009](ADR-009-full-stack-product.md); preserves [ADR-012](ADR-012-local-agent-tunnel.md)

## Context

The current milestones implement server-side traffic generation before the local agent. The intended first user instead needs a tool beside a backend, with a remote self-hosted dashboard. Requiring an in-process framework SDK would restrict adoption and couple load generation to the system being measured. Agent and worker documents also disagree about who analyzes and finalizes agent runs.

## Decision drivers

- Framework-independent installation and target traffic that stays beside the backend.
- One authoritative lifecycle and deterministic evaluator.
- Remote control available only with identity, safety, and bounded evidence transfer.
- No additional project, broker, or plugin marketplace.

## Options considered

| Option | Pros | Cons |
|---|---|---|
| Server worker first | Existing delivery order | Delays the developer-local workflow; remote measurements can misrepresent local behavior |
| Backend SDK | Framework-specific discovery and instrumentation | Changes the target process and needs multiple integrations |
| CLI/sidecar with self-hosted dashboard | Works beside different HTTP backends; isolates execution | Requires enrollment and a versioned control protocol early |

## Decision

Distribute `ScaleProof.Agent` as one `scaleproof` executable with command and persistent-agent modes. It is an integration tool, not code loaded into the target, an IDE extension, or an arbitrary-code plugin host. Do not add `ScaleProof.Cli`.

Use `local_agent` execution for the initial release. Defer `server_worker`, diagnostic tunnels, write flows, fleets, and additional protocols. A user must explicitly approve each read-only operation; a GET method alone is not proof of harmless behavior.

The server worker owns the attempt lease from dispatch through deterministic analysis and finalization. The gateway authenticates and relays bounded messages. The agent independently verifies signed instructions, maintains a durable execution fence, runs k6, and uploads normalized evidence. It has no database connection and cannot finalize authoritative findings. Neither the API nor the worker sends target traffic in this release.

Preserve the eight project boundaries, PostgreSQL queue, REST/SSE frontend, same-origin hosting, and outbound agent connection. Move deployed identity, project authorization, signed manifests, revocation, durable quotas, bounded uploads, and retention ahead of remote dashboard availability. The phased delivery order is owned by the product requirements and roadmap.

If accepted, this ADR supersedes only the server-execution-first responsibilities in ADR-002 and the separate thin CLI/initial server-execution product shape in ADR-009. Other provisions remain binding. Do not change their status or text before maintainer acceptance. ADR-012's local execution and no-performance-tunnel constraints remain in force.

## Consequences

The server worker becomes an orchestrator/analyzer with direct references to Application, Contracts, and Infrastructure for registration; it has no direct Execution adapter dependency. Local and remote launches use the same submission API and immutable scenario versions. Lease-loss, duplicate-dispatch, old-agent execution, upload authorization, and cancellation need integration tests before remote launch. Existing schemas may reserve later execution locations while admission rejects them in the initial release.

JCS, JWS ES256, HDR encoding, and the JSON agent envelope remain Proposed under ADR-013/014/015/018. Resolve these prerequisites before the corresponding runtime slice; this ADR does not silently accept their implementations.

## Revisit when

A documented user workload needs centrally managed target-facing execution or an SDK, with measurements showing why the local executable is insufficient. Any extension must retain the same safety and evidence contracts.
