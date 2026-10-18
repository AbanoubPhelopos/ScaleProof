# ADR-002: Modular monolith plus separate worker

Status: Accepted

## Context

The control plane needs cohesive transactions while workload execution needs process, resource, and network isolation. Premature microservices would add operational failure modes before boundaries are proven.

## Decision

Keep control-plane modules in one deployable API and run benchmarks in a separate worker process. Preserve logical module boundaries in code.

## Consequences

The API never runs long benchmarks inside requests. Deployment has at least API, worker, and PostgreSQL. Module contracts must avoid becoming an unstructured monolith.

## Revisit when

Independent scaling, ownership, security isolation, release cadence, or measured failure containment requires another deployable boundary.
