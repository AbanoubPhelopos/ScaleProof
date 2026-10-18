# ADR-001: Use k6 through an adapter

Status: Accepted

## Context

Workload scheduling, open/closed models, thresholds, and engine diagnostics are subtle. Rebuilding them would delay validation of ScaleProof's differentiating experiment workflow.

## Decision

Use a version-tested k6 adapter for initial HTTP execution. Normalize its output behind a ScaleProof contract and retain bounded original output for diagnosis.

## Consequences

The product depends on supported k6 behavior and must record exact versions. ScaleProof scenarios cannot promise semantics the adapter cannot faithfully translate. A contract-test suite isolates engine changes.

## Revisit when

A validated protocol/workload need cannot reasonably be expressed through k6, or k6 licensing/distribution/compatibility materially blocks the product.
