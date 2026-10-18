# ADR-010: OpenAPI-generated frontend contract

Status: Accepted

## Context

Handwritten TypeScript DTOs drift from C# around nullability, enums, errors, and pagination.

## Decision

Generate OpenAPI 3.1 from ASP.NET Core at build time and generate TypeScript client/types using a pinned tool. Verify deterministic output in CI. Keep scenario/report/check JSON Schemas as separate portable artifacts.

## Consequences

Endpoints require stable operation IDs and response metadata. Backend contract changes visibly update frontend code. Generated files are never manually edited; golden contract fixtures protect semantics.

## Revisit when

The chosen generator cannot express a required contract. Replace the generator behind OpenAPI rather than returning to duplicate handwritten DTOs.
