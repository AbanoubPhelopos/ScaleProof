# ADR-017: Aspire AppHost for the developer inner loop; Compose for clean-clone runs

Status: Proposed
Date: 2026-10-17
Related: NFR-005, NFR-006, [local development](../engineering/local-development.md), [ADR-009](ADR-009-full-stack-product.md)

## Context

M0 runs the API, worker, PostgreSQL, the React dev server, the sample API, and an OpenTelemetry collector together. Plain Docker Compose works, but it gives .NET contributors no debugger attach, no dashboard for the platform's own traces and logs, and duplicated connection-string wiring.

Aspire (the ".NET" prefix was dropped at version 13, November 2025) provides:

- an AppHost that orchestrates these resources
- a local OTLP dashboard
- service discovery
- generation of a Compose file from the same model

## Decision drivers

- Fast F5 inner loop for .NET contributors
- Observability of ScaleProof itself (NFR-006)
- No .NET SDK requirement for people who only want to try it

## Options considered

| Option | Pros | Cons |
|---|---|---|
| Docker Compose only | Universal; no SDK needed | Weak debugging; manual env wiring; no built-in dashboard |
| **Aspire AppHost + generated/maintained `compose.yaml`** | Debug, dashboard, typed wiring; `aspire publish` can emit Compose | One more project; Aspire versioning cadence |
| Tilt / Skaffold | Polyglot, Kubernetes-oriented | Kubernetes is a non-goal for M0–M2 |

## Decision

- Add `src/ScaleProof.AppHost`, a dev-only composition root that is never deployed. It adds PostgreSQL, API, Worker, the Vite app (via the Aspire JavaScript hosting integration), the sample API, and the OTel collector.
- Keep a checked-in **`compose.yaml`** for "clone and run" without the .NET SDK. Generate it with `aspire publish` where the output is acceptable. Otherwise maintain it by hand, and add a CI job that boots it and runs the smoke scenario (NFR-005).
- Production deployment is not defined by Aspire.

## Consequences

- The blueprint's "eight .NET projects" becomes nine, plus `ScaleProof.ServiceDefaults` if used. This ADR is the explicit exception to that rule.
- Pin the Aspire version in `Directory.Packages.props`.

## Revisit when

Aspire stops supporting the needed resources, or Compose drift between the two paths causes repeated CI failures.
