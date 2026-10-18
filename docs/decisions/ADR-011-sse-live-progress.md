# ADR-011: Persisted SSE for live progress

Status: Accepted

## Context

Run progress is server-to-client, moderate-frequency, and must reconnect across API/worker processes. WebSockets add bidirectional lifecycle complexity without a current requirement.

## Decision

Use Server-Sent Events backed by bounded persisted progress events. Support sequence and `Last-Event-ID`; recover with summary refetch and capped polling.

## Consequences

Payloads stay small and contain no raw logs/evidence. Persistence enables cross-process delivery and reconnect but needs retention. Cancel and other commands remain normal HTTP.

## Revisit when

Validated features require high-frequency or bidirectional communication that SSE plus HTTP commands cannot meet.
