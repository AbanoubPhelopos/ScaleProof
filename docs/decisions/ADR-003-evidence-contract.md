# ADR-003: Evidence and uncertainty are first-class

Status: Accepted

## Context

Black-box traffic cannot universally prove N+1 behavior, business idempotency, or root cause. Binary pass/fail would overstate weak or missing evidence.

## Decision

All checks use a versioned contract containing status, severity, confidence, evidence, limitations, and recommendations. Missing evidence produces `inconclusive`, `not_applicable`, `skipped`, or `error` as appropriate—not a silent pass.

## Consequences

Reports and CI must support more than green/red. Each check needs deterministic evidence rules and fixtures. Severity and confidence remain independent.

## Revisit when

The schema evolves to support calibrated probability or richer evidence, while retaining the distinction between observation quality and operational impact.
