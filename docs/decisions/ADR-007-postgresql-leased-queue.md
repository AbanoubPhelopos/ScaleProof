# ADR-007: PostgreSQL lease-based queue for MVP

Status: Accepted

## Context

The first release needs durable dispatch, duplicate-delivery safety, cancellation, and recovery, but not the operational burden of a broker.

## Decision

Use PostgreSQL transactional claiming with one active lease per run, attempt IDs, expiry, and heartbeat. Keep dispatch behind an application boundary so transport can change later.

## Consequences

Critical claim transitions may use explicit SQL and concurrency-focused integration tests. Database availability affects dispatch. Backlog and polling behavior must be bounded and observable.

## Revisit when

Measured queue volume, latency, fan-out, multi-region needs, or independent scaling makes PostgreSQL dispatch inadequate.
