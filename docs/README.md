# ScaleProof design guide

Status: Informative

ScaleProof tests backend HTTP APIs. A separately installed `scaleproof` CLI/sidecar runs beside the backend; a self-hosted React dashboard reviews scenarios, controls runs, and presents evidence. AI can draft scenarios and explain results using the developer's locally configured key. It never decides whether a measurement passes.

This is the **proposed target design**, not shipped behavior. The repository currently contains .NET scaffolding. [ADR-022](decisions/ADR-022-plugin-first-delivery.md) and [ADR-023](decisions/ADR-023-local-ai-assistance.md) record the revision. Accepted ADRs remain binding until a maintainer accepts their replacements; unchanged measurement and safety invariants continue to apply. No application implementation or ADR acceptance is implied by these documentation changes.

## Read in this order

1. [Product requirements](product/product-requirements.md): audience, scope, requirement IDs, and release boundaries.
2. [Architecture overview](architecture/overview.md): topology and end-to-end data flow.
3. [Implementation blueprint](architecture/implementation-blueprint.md): project boundaries and invariants.
4. [Local agent](architecture/local-agent-and-tunnel.md) and [AI assistance](specs/ai-assistance.md): local execution, onboarding, generation, and explanations.
5. [Roadmap](delivery/roadmap-and-quality.md): ordered slices and acceptance gates.

## Ownership and evidence

| Topic | Single source of truth |
|---|---|
| Terms | [Glossary](glossary.md) |
| Run state and lease ownership | [Run lifecycle](specs/run-lifecycle.md) |
| Scenario validation | [Scenario v1](specs/scenario-v1.md) |
| AI input, review, and explanation lifecycle | [AI assistance](specs/ai-assistance.md) |
| Public HTTP and agent interfaces | [API reference](specs/api-reference.md), [events and protocol](specs/events-and-agent-protocol.md) |
| Defaults and resource budgets | [Configuration](specs/configuration.md) |
| Measurements, findings, reports | [Metrics](specs/metrics-and-statistics.md), [findings](specs/finding-v1.md), [reports](specs/report-v1.md) |
| Threats and acceptance coverage | [Threat model](architecture/threat-model.md), [testing strategy](engineering/testing-strategy.md) |

HTTP observations identify slow/error-prone endpoints under the tested load. Database, dependency, CPU, and memory explanations require corresponding telemetry. Missing evidence stays unavailable; an AI hypothesis is never a confirmed root cause.
