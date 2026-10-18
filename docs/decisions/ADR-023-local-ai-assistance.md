# ADR-023: Local AI drafts and evidence-linked explanations

Status: Proposed
Date: 2026-10-18
Deciders: maintainers
Related: FR-048–FR-051, NFR-012; [ADR-022](ADR-022-plugin-first-delivery.md), [AI assistance](../specs/ai-assistance.md), [ADR-003](ADR-003-evidence-contract.md), [ADR-008](ADR-008-versioned-immutable-contracts.md)

## Context

Developers want scenarios appropriate to their API and help interpreting bottlenecks. AI output is non-deterministic and can invent endpoints, data, thresholds, or root causes. Sending an application repository, credentials, or raw traffic to a centralized model would create unnecessary disclosure and operational dependencies.

## Decision drivers

- Developer-owned AI endpoint, model, and local environment key.
- Explicit context disclosure and human review before execution.
- Deterministic test outcomes independent of model availability.
- Evidence-linked advice without claims beyond the observed data.

## Options considered

| Option | Pros | Cons |
|---|---|---|
| Central server key and generation | Simple remote scheduling | Centralizes credentials and source context |
| Local AI with review | Keeps credentials local and makes disclosure inspectable | Requires an online capable agent and explicit context approval |
| Autonomous AI testing | Less manual interaction | Model mistakes can initiate unsafe or irrelevant traffic |

## Decision

Run AI calls inside the local executable. Support one documented chat-completions-compatible request/response profile initially, with endpoint, model, and API key from local environment configuration. A key is required only for AI features. Manual scenarios, execution, deterministic analysis, and reporting must work without it.

Use locally imported OpenAPI 3.0/3.1 JSON or YAML and developer-selected operations, synthetic dataset references, authentication references, workload goals, and SLOs. Reject external references initially. No source-code scanning, traffic capture, automatic endpoint crawling, executable model tools, or autonomous workload execution.

Show sanitized model context and its destination before approval on the local machine. Treat model output and API descriptions as untrusted data. Publish a reviewed, validated scenario as an immutable version; submission remains a separate action.

The server evaluates evidence deterministically. AI explanations are optional, separately versioned, evidence-linked artifacts. They never change lifecycle, quality, outcome, findings, or comparison verdicts. Preserve failed AI jobs independently of successful measurements. Do not promise bit-for-bit model reproducibility; retain provenance and the approved output.

## Consequences

Implement bounded model calls and schema validation inside Agent feature modules without a new service/project. The key is never sent to the dashboard or inherited by k6 or the Collector. Local consent is tied to exact sanitized input and endpoint/model; remote messages cannot approve new disclosure. The specification defines retry, failure, retention, and review behavior. Provider compatibility must be demonstrated with a fake server and a documented opt-in smoke test, not inferred from a vendor label.

## Revisit when

Users require a named provider's native protocol or centrally administered inference, with a documented incompatibility or operational requirement. Add adapters or centralized execution only through an explicit credential and disclosure design review.
