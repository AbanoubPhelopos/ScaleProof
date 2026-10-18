# ADR-021: Keep the purpose-built PostgreSQL lease queue (library comparison)

Status: Proposed
Date: 2026-10-17
Related: FR-032, NFR-001, [ADR-007](ADR-007-postgresql-leased-queue.md), [database schema](../specs/database-schema.md#lease-sql)

## Context

[ADR-007](ADR-007-postgresql-leased-queue.md) accepted a PostgreSQL lease-based queue but did not record why existing .NET job libraries were rejected. Contributors will reasonably ask "why not Hangfire?".

## Options considered

| Option | Pros | Cons for ScaleProof |
|---|---|---|
| **Purpose-built `FOR UPDATE SKIP LOCKED` claim on `run_attempt`** | Lease token, attempt ordinal, quality status, mutation no-replay, and quota admission live in the same rows and transaction; about 150 lines of SQL plus tests | We own the correctness tests |
| Hangfire (PostgreSQL storage) | Mature, dashboard | Automatic retries conflict with invariant 8 (no mutation replay); job state is separate from run state; serialized method calls are not our contract |
| Wolverine (PostgreSQL durability) | Durable inbox/outbox, good .NET fit | A whole messaging framework for one queue; its own retry and error semantics to override |
| Quartz.NET / TickerQ | Scheduling | Schedulers, not leased work claims |
| Outbox + RabbitMQ/NATS | Scales out | New infrastructure; ADR-002 forbids a broker without measured need |

## Decision

Keep the purpose-built queue. The exact SQL is normative in [database schema](../specs/database-schema.md#lease-sql). Recurring housekeeping (lease-expiry sweeps, retention, orphan cleanup) runs as `BackgroundService` timers in the worker with `PeriodicTimer`. No scheduler library is used.

## Revisit when

Same as ADR-007, or a fourth kind of background job appears that would benefit from a general scheduler.
