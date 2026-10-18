# ADR-004: Mutation checks require explicit invariants and isolation

Status: Accepted

## Context

Response equality does not prove exactly-once business effects, and replay can damage real data. Arbitrary validators or SQL also create a code/data access boundary.

## Decision

Idempotency and concurrency checks require a declared logical request, isolated target, deterministic fixture, bounded replay, explicit read-only probe and assertion, observation window, and reset/reconciliation behavior. They are denied for production-class targets by default.

## Consequences

Configuration is more involved but conclusions are defensible. Missing probes are inconclusive. Started mutation attempts cannot be blindly retried.

## Revisit when

Trusted framework adapters or disposable-environment integrations can provide equivalent evidence with a simpler safe workflow.
