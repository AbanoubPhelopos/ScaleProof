# Roadmap and quality gates

Status: Proposed (ADR-022, ADR-023); replaces the server-execution-first delivery order if accepted.
Audience: maintainers and implementers
Related: [release scope](../product/product-requirements.md#release-scope), [stories](../product/user-stories.md), [testing](../engineering/testing-strategy.md)

Milestones describe target behavior, not delivered code. Complete vertical slices through contract, domain/application, adapters, API, generated client, UI, and relevant tests. Resolve ADR-013/014/015/018 before their dependent runtime work. No code generation, package choice, or signature format becomes Accepted through this roadmap.

## M0 — Local walking skeleton

M0 completes the plugin and evidence-dashboard workflow. Deliver the stages in order; remote availability and AI cannot precede their safety gates.

| Stage | Slices | Traceability |
|---|---|---|
| A. Safe manual local run | Scenario/evidence/report contracts and fixtures; persistence/leases; one Agent executable with CLI/service modes; local session and enrollment; signed dispatch/start fencing; policy; bounded read-only k6; generator diagnostics; server deterministic analysis; JSON/Markdown; disposable sample API and Compose | FR-001–FR-004, FR-010–FR-013, FR-016, FR-020–FR-023, FR-030–FR-033, FR-046–FR-047; US-001–US-006, US-023, US-030–US-034 |
| B. Secured remote dashboard | OIDC/project RBAC, same-origin web, mTLS rotation/revocation, quotas/rate controls, upload grants/S3, retention/deletion, SSE/cancel, endpoint charts with accessible tables and route states | FR-034, FR-040–FR-043, FR-045–FR-047; NFR-003/004/008/009/011; US-014, US-042 |
| C. Reviewed local AI | Local OpenAPI import, sanitized disclosure/consent, bounded model protocol, draft revision/review/publication, independent run submission, separate evidence explanations and safe failure UX | FR-048–FR-051, NFR-012; US-043–US-045 |
| D. Optional diagnostics | Selected Collector OTLP export into bounded local receiver; run/request correlation; exact-process/container opt-in; repeated-query/dependency evidence and limitations | FR-018, FR-024; US-008, US-020, US-024, US-046 |

**Stage A exit:** clean local fixture validates, runs, cancels, and produces reports without an AI key. Invalid scenarios/destinations/caps send zero workload requests. Duplicate start and worker/agent crash races cannot overlap attempts. Generator saturation never passes. Seeded secrets are absent from every persisted output and child environment.

**Stage B remote-access gate:** do not expose the dashboard beyond loopback until OIDC and the project authorization matrix, enrollment/revocation, signed-manifest tamper/replay tests, stale-worker fencing, upload hash/size tests, quotas, retention/deletion, TLS/proxy controls, and cancellation/disconnect tests pass. Self-hosting does not waive this gate. Record the deployment configuration and reproducible results.

**Stage C exit:** a synthetic local OpenAPI document produces a reviewed valid scenario and a separately submitted run. Inference sees only consented sanitized input; unsafe/invented operations, secrets, missing fixtures, stale approval, and malformed output are blocked. A fake provider's 401/429/5xx, timeout, and cancellation do not retry automatically or alter measured outcomes. Explanation references resolve and model text cannot execute markup.

**Stage D / first-release exit:** HTTP-only reports explicitly show missing telemetry; instrumented slow-dependency and repeated-query fixtures show evidence-linked findings, while the optimized fixture is not flagged. Measure and record instrumentation/generator overhead and sampling coverage. An unfamiliar developer follows the documented enrollment/import/review/run/dashboard journey without exposing the target. Record usability timing rather than promising an unmeasured onboarding duration.

## M1 — Compare and gate

Implement baselines, compatibility-first comparisons, fixed-threshold deltas, repetitions/variance, bounded step/capacity profiles, CI integration, and comparison UI (FR-014/022/026/044; US-010–US-013/015). ADR-016 statistics remain optional pending acceptance.

Exit: controlled latency/error regressions are detected; an incompatible baseline receives no regression verdict; inadequate samples remain inconclusive. Record false-positive behavior across repeated no-change runs with workload/environment context, rather than treating one perfect trial count as a universal guarantee.

## M2 — Operational maturity

Validate additional signed binary platforms, upgrade compatibility, backup/restore, dispatch/SSE scale, quota pressure, telemetry overhead, and agent update/recovery. Choose new infrastructure only from measured need and a dedicated ADR. Earlier security controls remain M0 prerequisites.

## M3+ — Advanced

Separately specify mutation approvals, isolated data/reset/cleanup, stateful journeys, replay/idempotency probes, diagnostic tunnels, server execution, private pools, additional protocols/engines, and third-party isolation. Reserved enum values and historical examples do not enable these features.

## Public-release prerequisites

- License/community files exist; fill the real security contact and maintainer list before release.
- Verify installation and quickstart on every advertised platform using real CI. Publish actual supported versions, signed archives/checksums, SBOM/provenance, and rollback guidance.
- No runtime quickstart command is claimed working while the repository is scaffolding.
- No performance or root-cause claim without reproducible workload, environment, normalized evidence, and explicit limitations.
