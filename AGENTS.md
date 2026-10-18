# AGENTS.md

These are operating instructions for AI coding agents (and humans) working in this repository. The format follows the [agents.md](https://agents.md) convention. Tool-specific files (`CLAUDE.md`, `.github/copilot-instructions.md`, …) should only point here.

If `GITHUB_RULES.md` exists in this checkout, read it before phase, commit, review,
or PR work. It contains local workflow instructions and is intentionally ignored
by Git; checkouts without it remain usable. It does not change specification
precedence, accept Proposed ADRs, or configure GitHub enforcement.

## What this project is

ScaleProof is a self-hosted tool that turns backend load tests into reproducible experiments:

1. validate a scenario and target **before** any traffic
2. run k6 under enforced caps (proposed target: local CLI/sidecar from M0; ADR-022)
3. normalize evidence
4. evaluate thresholds and checks
5. compare against a **compatible** baseline
6. emit JSON/Markdown reports and stable CLI exit codes

It tests **backend services only**. It is not a browser or frontend performance tool.

**License:** Apache-2.0 ([LICENSE](LICENSE), [NOTICE](NOTICE)); k6 (AGPL-3.0) is invoked as a separate, unmodified process. Community files: [CONTRIBUTING](CONTRIBUTING.md), [Code of Conduct](CODE_OF_CONDUCT.md), [SECURITY](SECURITY.md), [GOVERNANCE](GOVERNANCE.md), [CHANGELOG](CHANGELOG.md). Competitive positioning: [competitive analysis](docs/product/competitive-analysis.md).

**Design revision:** [docs guide](docs/README.md), [ADR-022](docs/decisions/ADR-022-plugin-first-delivery.md), and [ADR-023](docs/decisions/ADR-023-local-ai-assistance.md) describe the proposed plugin-first workflow and local AI assistance. Accepted ADRs remain binding until maintainer acceptance; the documentation revision does not implement the product.

**Status:** the specs are ahead of the code. `src/` contains empty scaffolding for 8 .NET 10 projects. The current target is **milestone M0** ([roadmap](docs/delivery/roadmap-and-quality.md#m0--local-walking-skeleton)).

## Read before you code

| Task | Read |
|---|---|
| Anything | [glossary](docs/glossary.md) (lifecycle ≠ quality ≠ outcome; severity ≠ confidence) and the [implementation blueprint](docs/architecture/implementation-blueprint.md) |
| Run state, workers, leases, cancellation | [run lifecycle](docs/specs/run-lifecycle.md), [database schema → lease SQL](docs/specs/database-schema.md#lease-sql) |
| AI input/review/explanations | [AI assistance](docs/specs/ai-assistance.md) |
| Endpoints | [API reference](docs/specs/api-reference.md), [error codes](docs/specs/error-codes.md), [backend architecture](docs/architecture/backend-architecture.md) |
| Scenarios and validation | [scenario v1](docs/specs/scenario-v1.md), [`schemas/scenario/v1.schema.json`](schemas/scenario/v1.schema.json) |
| k6 execution | [k6 adapter](docs/specs/k6-adapter.md), [metrics and statistics](docs/specs/metrics-and-statistics.md) |
| Checks and findings | [finding v1](docs/specs/finding-v1.md) |
| Reports, comparison | [report v1](docs/specs/report-v1.md), [comparison](docs/specs/comparison.md) |
| Frontend | [frontend architecture](docs/architecture/frontend-architecture.md), [SSE events](docs/specs/events-and-agent-protocol.md#sse-run-events) |
| Agent / gateway (proposed M0) | [local agent](docs/architecture/local-agent-and-tunnel.md), [agent protocol](docs/specs/events-and-agent-protocol.md#agent-control-protocol) |
| AI import, draft review, explanations | [AI assistance](docs/specs/ai-assistance.md), [local AI configuration](docs/specs/configuration.md#local-ai-options--proposed-adr-023) |
| Limits, config | [configuration](docs/specs/configuration.md) |
| Security-relevant code | [threat model](docs/architecture/threat-model.md) |
| Coding conventions | [engineering standards](docs/engineering/standards.md), [testing strategy](docs/engineering/testing-strategy.md) |

If two documents disagree, the precedence is:

1. Accepted ADR
2. `docs/specs/*`
3. `docs/architecture/*`
4. everything else

Report the conflict in your summary.

## Repository map

```text
src/ScaleProof.Domain          entities, value objects, state rules (no dependencies)
src/ScaleProof.Application     use cases, ports, checks, statistics (→ Domain)
src/ScaleProof.Contracts       public DTOs/events, enums, JSON metadata (no business rules)
src/ScaleProof.Execution       local k6 adapter, plan, redaction, caps (used by Agent; Proposed ADR-022)
src/ScaleProof.Infrastructure  EF Core/PostgreSQL, artifact stores, signing
src/ScaleProof.Api             Minimal API endpoints, authz, OpenAPI, SSE (composition root)
src/ScaleProof.Worker          claims/orchestrates, analyzes, finalizes (Proposed ADR-022)
src/ScaleProof.Agent           CLI/sidecar, proposed M0 (never references Api/Application/Infrastructure)
web/scaleproof-web             React 19 + TS SPA (planned)
schemas/                       JSON Schemas for public contracts
docs/                          specs and design
```

Dependency rules are enforced by architecture tests ([blueprint](docs/architecture/implementation-blueprint.md#dependency-rules)).

## Commands

```bash
dotnet build ScaleProof.sln                     # verified: builds the scaffolding
dotnet test                                     # planned: no test projects yet
```

The full planned workflow is in [local development](docs/engineering/local-development.md). Never claim that a command passed unless you ran it and saw it pass.

## Workflow: vertical slices

Build in this order, and finish the whole slice before stopping:

1. Contract or schema (`Contracts`, `schemas/`)
2. Domain rule
3. Application handler
4. Persistence or adapter
5. API endpoint
6. Regenerated frontend client
7. React feature
8. Tests at each layer

**Definition of done:**

- The behavior matches the spec. If the spec was wrong or incomplete, update it in the same change.
- Tests cover the happy path, invalid input, authorization, cancellation/idempotency, and redaction where relevant.
- `dotnet build` passes with no warnings (warnings are errors). Relevant tests pass.
- OpenAPI and schema changes are regenerated and diffed.
- The summary lists the commands you actually ran, plus any assumptions and open risks.

## Never do

These come from the non-negotiable invariants in the [blueprint](docs/architecture/implementation-blueprint.md#non-negotiable-invariants):

- Send target traffic before schema, authorization, destination, and limit validation succeed.
- Interpolate scenario text into JavaScript, shell commands, SQL, or file paths. k6 receives a static template plus a JSON plan.
- Represent missing data as `0` or `pass`. Merge samples across attempts. Average percentiles.
- Auto-retry a mutation after traffic may have been sent.
- Log, persist, or return secret values. Use references and redaction.
- Let an endpoint skip project-scoped authorization, including in local mode, where the abstractions must still exist.
- Edit `web/scaleproof-web/src/api/generated/**` by hand.
- Rewrite an Accepted ADR. Write a superseding one instead ([template](docs/decisions/template.md)).
- Implement behavior defined only in a **Proposed** ADR unless the task asks for it.
- Add a dependency, project, broker, cache, or abstraction layer (MediatR, generic repositories, FluentValidation, Redux, …) without an ADR.
- Commit secrets, real hostnames, or personal data in fixtures.

## Proposing changes

- **Design change:** add `docs/decisions/ADR-NNN-*.md` with status Proposed, using the template. Link it from the affected spec, labeled "Proposed (ADR-NNN)".
- **New requirement:** append a new `FR-`/`NFR-` ID. Never renumber or reuse IDs.
- **New error code or termination reason:** add it to [error codes](docs/specs/error-codes.md) in the same change.

## Document status legend

Every document starts with a `Status:` line.

| Status | Meaning |
|---|---|
| **Normative** | Implementations must conform. Changing it requires a PR that updates the code and the tests together. |
| **Proposed** | A recommendation under review, normally backed by a Proposed ADR. Do not implement it unless the task says to. |
| **Informative** | Background, rationale, or guidance. |

## Reading paths

| You are… | Read in this order |
|---|---|
| **A coding agent** | this file → [glossary](docs/glossary.md) → [implementation blueprint](docs/architecture/implementation-blueprint.md) → the spec for your task ([map](#specifications-normative-implement-from-these)) → [standards](docs/engineering/standards.md) |
| New contributor | [product requirements](docs/product/product-requirements.md) → [architecture overview](docs/architecture/overview.md) → [roadmap](docs/delivery/roadmap-and-quality.md) → [local development](docs/engineering/local-development.md) → [CONTRIBUTING](CONTRIBUTING.md) |
| Backend implementer | [backend architecture](docs/architecture/backend-architecture.md) → [run lifecycle](docs/specs/run-lifecycle.md) → [database schema](docs/specs/database-schema.md) → [API reference](docs/specs/api-reference.md) → [k6 adapter](docs/specs/k6-adapter.md) |
| Frontend implementer | [frontend architecture](docs/architecture/frontend-architecture.md) → [API reference](docs/specs/api-reference.md) → [SSE events](docs/specs/events-and-agent-protocol.md#sse-run-events) → [report v1](docs/specs/report-v1.md) |
| Security reviewer | [threat model](docs/architecture/threat-model.md) → [security and operations](docs/architecture/security-and-operations.md) → [local agent](docs/architecture/local-agent-and-tunnel.md) |
| Operator | [deployment](docs/architecture/deployment-architecture.md) → [configuration](docs/specs/configuration.md) → [security and operations](docs/architecture/security-and-operations.md#health-and-runbooks) |
| Evaluator | [competitive analysis](docs/product/competitive-analysis.md) → [product requirements](docs/product/product-requirements.md) |

## Documentation map

### Product (why and what)

- [Product requirements](docs/product/product-requirements.md): vision, FR/NFR IDs, milestones M0–M3
- [User stories](docs/product/user-stories.md): acceptance criteria, tagged by milestone
- [Competitive analysis](docs/product/competitive-analysis.md): landscape, reuse, positioning

### Architecture (how, at the structural level)

- [Overview](docs/architecture/overview.md): C4 context and containers, modules, data flow
- [Implementation blueprint](docs/architecture/implementation-blueprint.md): projects, dependency rules, invariants, fixed technology
- [Backend](docs/architecture/backend-architecture.md) · [Frontend](docs/architecture/frontend-architecture.md) · [Data and contracts](docs/architecture/data-and-contracts.md)
- [Local agent and tunnel](docs/architecture/local-agent-and-tunnel.md) · [Deployment](docs/architecture/deployment-architecture.md)
- [Security and operations](docs/architecture/security-and-operations.md) · [Threat model](docs/architecture/threat-model.md)

### Specifications (normative, implement from these)

| Spec | Covers |
|---|---|
| [Run lifecycle](docs/specs/run-lifecycle.md) | States, transitions, leases, quality status |
| [Scenario v1](docs/specs/scenario-v1.md) | Scenario fields, validation order, canonicalization |
| [Metrics and statistics](docs/specs/metrics-and-statistics.md) | Metric catalog, percentiles, sample adequacy, quality policy |
| [k6 adapter](docs/specs/k6-adapter.md) | Script template, invocation, ingestion, cancellation, exit codes |
| [Finding v1](docs/specs/finding-v1.md) | Check contract, statuses, how to author a check |
| [Report v1](docs/specs/report-v1.md) | JSON report and Markdown rendering |
| [Comparison](docs/specs/comparison.md) | Compatibility rules, deltas, statistical verdict |
| [API reference](docs/specs/api-reference.md) | Endpoints, conventions, idempotency, pagination |
| [Events and agent protocol](docs/specs/events-and-agent-protocol.md) | SSE events, agent envelope and messages, manifest |
| [Error codes](docs/specs/error-codes.md) | Problem codes, termination reasons, CLI exit codes |
| [Database schema](docs/specs/database-schema.md) | Tables, ERD, normative lease SQL |
| [AI assistance](docs/specs/ai-assistance.md) | Proposed local inference, disclosure, scenario drafts, independent explanations |
| [Configuration](docs/specs/configuration.md) | Options and **all default limits and quotas** |
| JSON Schemas | [`schemas/`](schemas) (scenario, report, check-result) |

### Engineering and delivery

- [Engineering standards](docs/engineering/standards.md) · [Testing strategy](docs/engineering/testing-strategy.md) · [Release and versioning](docs/engineering/release-and-versioning.md) · [Local development](docs/engineering/local-development.md)
- [Roadmap and quality gates](docs/delivery/roadmap-and-quality.md)
- [Glossary](docs/glossary.md)

## Single source of truth

Every fact lives in exactly one place. Other documents link to it instead of restating it.

| Topic | Owner document |
|---|---|
| Run states and transitions | [run lifecycle](docs/specs/run-lifecycle.md) |
| Default limits, rate limits, quotas | [configuration](docs/specs/configuration.md) |
| Technology choices | [implementation blueprint](docs/architecture/implementation-blueprint.md#fixed-technology-choices) |
| Non-negotiable invariants | [implementation blueprint](docs/architecture/implementation-blueprint.md#non-negotiable-invariants) |
| Error and exit codes | [error codes](docs/specs/error-codes.md) |
| AI input/review/explanations | [AI assistance](docs/specs/ai-assistance.md) |
| Endpoints | [API reference](docs/specs/api-reference.md), replaced by OpenAPI once code exists |
| Milestone scope | [product requirements](docs/product/product-requirements.md#release-scope) |

## Decision records

ADRs live in `docs/decisions/`. Accepted decisions are binding constraints. Proposed ones are under review.

| ADR | Decision | Status |
|---|---|---|
| [ADR-001](docs/decisions/ADR-001-use-k6.md) | Use k6 through an adapter | Accepted |
| [ADR-002](docs/decisions/ADR-002-modular-monolith-and-worker.md) | Modular monolith plus separate worker; [proposed partial successor](docs/decisions/ADR-022-plugin-first-delivery.md) | Accepted |
| [ADR-003](docs/decisions/ADR-003-evidence-contract.md) | Evidence and uncertainty are first-class | Accepted |
| [ADR-004](docs/decisions/ADR-004-safe-mutation-checks.md) | Mutation checks require explicit invariants and isolation | Accepted |
| [ADR-005](docs/decisions/ADR-005-versioned-reports.md) | JSON automation contract plus Markdown rendering | Accepted |
| [ADR-006](docs/decisions/ADR-006-compatible-comparisons-only.md) | Compatibility gates comparison | Accepted |
| [ADR-007](docs/decisions/ADR-007-postgresql-leased-queue.md) | PostgreSQL lease-based queue for MVP | Accepted |
| [ADR-008](docs/decisions/ADR-008-versioned-immutable-contracts.md) | Versioned immutable experiment contracts | Accepted |
| [ADR-009](docs/decisions/ADR-009-full-stack-product.md) | Full-stack product; [proposed partial successor](docs/decisions/ADR-022-plugin-first-delivery.md) | Accepted |
| [ADR-010](docs/decisions/ADR-010-openapi-generated-client.md) | OpenAPI-generated frontend contract | Accepted |
| [ADR-011](docs/decisions/ADR-011-sse-live-progress.md) | Persisted SSE for live progress | Accepted |
| [ADR-012](docs/decisions/ADR-012-local-agent-tunnel.md) | Outbound local agent; tunnel diagnostics only | Accepted |
| [ADR-013](docs/decisions/ADR-013-rfc8785-canonicalization.md) | RFC 8785 JSON canonicalization for content hashes | Proposed |
| [ADR-014](docs/decisions/ADR-014-jws-signed-manifests.md) | JWS (ES256) signed execution manifests | Proposed |
| [ADR-015](docs/decisions/ADR-015-hdr-histograms-for-latency.md) | HDR histograms as latency evidence | Proposed |
| [ADR-016](docs/decisions/ADR-016-comparison-statistics.md) | Mann–Whitney U + bootstrap CI comparison method | Proposed |
| [ADR-017](docs/decisions/ADR-017-aspire-dev-orchestration.md) | Aspire AppHost for dev loop; Compose for clean clone | Proposed |
| [ADR-018](docs/decisions/ADR-018-agent-protocol-messages.md) | JSON Schema–typed agent messages over WebSocket | Proposed |
| [ADR-019](docs/decisions/ADR-019-frontend-codegen.md) | `@hey-api/openapi-ts` frontend codegen | Proposed |
| [ADR-020](docs/decisions/ADR-020-license-and-supply-chain.md) | Apache-2.0, k6 process boundary, supply-chain controls | Proposed |
| [ADR-021](docs/decisions/ADR-021-keep-postgresql-lease-queue.md) | Keep purpose-built PostgreSQL lease queue (vs Hangfire/Wolverine) | Proposed |
| [ADR-022](docs/decisions/ADR-022-plugin-first-delivery.md) | Local CLI/sidecar first; worker orchestration and deterministic analysis; proposed partial supersession of ADR-002/009 | Proposed |
| [ADR-023](docs/decisions/ADR-023-local-ai-assistance.md) | Local AI credentials, reviewed drafts, independent evidence explanations | Proposed |

### ADR lifecycle

Do not rewrite accepted ADR history to hide a change. Add a superseding ADR, update the table, and link both records. An ADR contains context, decision, consequences, and an objective revisit trigger.

New ADRs use [template](docs/decisions/template.md) (MADR-style, with an **Options considered** table). Their lifecycle:

1. **Proposed:** opened in a PR. Specs may reference it, but they must label the affected text "Proposed (ADR-NNN)".
2. **Accepted:** a maintainer merges the status change. Specs then drop the label.
3. **Rejected** or **Superseded:** the file is kept and linked from the replacement.

Coding agents must not implement behavior that only a Proposed ADR describes unless the task explicitly says to. They may implement Accepted behavior.

## Open questions

These decisions block the revised delivery slices. The Proposed target design does not silently accept its dependencies.

| # | Question | Blocks | Proposed answer |
|---|---|---|---|
| 1 | Accept ADR-022/023 and the partial supersession of ADR-002/009? | Revised product delivery | Local CLI/sidecar with self-hosted dashboard and optional local AI |
| 2 | Accept or reject ADR-013 (JCS) and ADR-015 (HDR)? | M0 contracts/execution | Resolve before implementing evidence/hash fixtures |
| 3 | Accept ADR-014 (JWS) and ADR-018 (agent envelope), including revised dispatch fencing? | M0 agent execution | Resolve before agent/runtime protocol implementation |
| 4 | Accept ADR-017 (Aspire)? | Optional developer orchestration | Compose remains the clean-clone path |
| 5 | Accept ADR-016 statistics? | Optional M1 statistics | Fixed-threshold comparisons first |
| 6 | Which accessible primitives and generated client tooling? | M0 React implementation | Choose by ADR; ADR-019 remains Proposed |
| 7 | Security contact and maintainer list | Public release | Owner must supply real contacts |
| 8 | Which S3-compatible service for the remote reference deployment? | M0 remote deployment | Choose/test explicitly; existing S3 adapter contract is retained |

CLI commands use explicitly granted project scopes on the enrolled agent identity in the proposed initial design; PAT/device-flow support is deferred. Build identity flags and CI auto-detection remain an M1 implementation detail requiring specification before code.
