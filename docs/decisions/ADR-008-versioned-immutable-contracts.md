# ADR-008: Versioned immutable experiment contracts

Status: Accepted

## Context

Reproducibility fails when scenarios, policies, target profiles, evidence definitions, or reports change underneath historical runs.

## Decision

Snapshot and hash exact scenario, target, effective policy, tool/engine, environment, and schema identities. Completed results are immutable except through explicit versioned redaction/deletion. Public scenario, finding, report, and adapter contracts are versioned.

## Consequences

Storage and migration cost increases, but historical claims remain auditable. Canonicalization and compatibility rules require golden tests. Corrections create new artifacts/versions rather than silent mutation.

## Revisit when

Never for the reproducibility principle; implementation/storage details may change through a superseding ADR.
