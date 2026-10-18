# ADR-009: Full-stack product with API and worker

Status: Accepted

## Context

ScaleProof's value is the complete experiment lifecycle: configuration, safety, durable execution, evidence review, comparison, and reporting. A backend-only plugin service would leave users to rebuild that workflow and weaken the product's consistency.

## Decision

Build a full-stack deployed product using .NET 10 LTS, ASP.NET Core Minimal APIs, a separate .NET worker, a separately distributed local agent, and a React 19 TypeScript/Vite SPA. React and automation use the same API. Adapter/plugin contracts remain internal process boundaries until a safe extension model is proven.

## Consequences

Backend and frontend have independent builds but one wire contract. The worker preserves execution isolation. Same-origin production hosting simplifies cookies, antiforgery, CORS, and deployment. The team maintains C# and TypeScript toolchains.

## Revisit when

A validated integration-only customer segment needs a headless distribution. That may add a supported headless mode; it does not remove the full product. External plugin hosting requires a separate trust/isolation ADR.
