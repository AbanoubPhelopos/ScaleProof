# ADR-019: `@hey-api/openapi-ts` for the generated frontend client

Status: Proposed
Date: 2026-10-17
Related: FR-041, [ADR-010](ADR-010-openapi-generated-client.md), [frontend architecture](../architecture/frontend-architecture.md#contracts)

## Context

[ADR-010](ADR-010-openapi-generated-client.md) requires a pinned generator but does not name one. The frontend uses TanStack Query and Zod. Hand-writing query hooks and schemas around plain types is exactly the drift ADR-010 tries to prevent.

## Options considered

| Option | Pros | Cons |
|---|---|---|
| **@hey-api/openapi-ts** | Types + fetch SDK + plugins for TanStack Query options and Zod schemas; OpenAPI 3.1 support; active | Pre-1.0; config surface changes between minors |
| Orval | Mature; React Query + Zod + MSW mock generation | Larger generated surface; mock generation overlaps our hand-written MSW handlers |
| openapi-typescript + openapi-fetch | Minimal, types-only, very stable | No query/Zod generation; we'd hand-write hooks |
| NSwag (TypeScript) | .NET-native | Class-based clients; weaker 3.1 and TanStack fit |

## Decision

- Use `@hey-api/openapi-ts`, **pinned to an exact version** with the `@hey-api/client-fetch`, `@tanstack/react-query`, and `zod` plugins. Output goes to `web/scaleproof-web/src/api/generated`.
- The custom `api/client.ts` configures base URL, credentials, correlation and antiforgery headers, and Problem Details parsing.
- CI regenerates and fails on diff.
- Fallback: openapi-typescript + openapi-fetch if hey-api's churn costs more than one maintenance day per quarter.

## Revisit when

Upgrades repeatedly break generated output, or the project reaches 1.0 with a materially different plugin model.
