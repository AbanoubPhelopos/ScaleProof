# ADR-005: JSON automation contract plus Markdown rendering

Status: Accepted

## Context

CI and integrations require stable machine data while contributors need a reviewable report. A dashboard would expand the first release without proving the workflow.

## Decision

Make versioned JSON the canonical report contract and Markdown the initial human rendering. Both derive from the same immutable normalized evidence.

## Consequences

Schema compatibility, golden fixtures, and migration rules are release concerns. Markdown cannot introduce conclusions absent from JSON. HTML and PR UI remain optional renderers.

## Revisit when

User evidence shows another rendering materially improves adoption without compromising the canonical contract.
