# ADR-006: Compatibility gates comparison

Status: Accepted

## Context

Workload, engine, target, dataset, cache, authentication, environment, or telemetry drift can dominate a code change and create false regression claims.

## Decision

Classify a baseline/candidate pair as `compatible`, `conditionally_comparable`, or `incompatible` with reasons before computing a policy verdict. Incompatible pairs do not produce a pass/fail regression claim.

## Consequences

Runs must capture richer environment metadata. Some desired comparisons will be refused or qualified. CI policy must handle conditional/inconclusive outcomes.

## Revisit when

Validated normalization or controlled infrastructure makes a specific cross-environment comparison defensible; encode that as an explicit compatibility rule.
