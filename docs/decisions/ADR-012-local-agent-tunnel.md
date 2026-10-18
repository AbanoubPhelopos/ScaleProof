# ADR-012: Outbound local agent; tunnel diagnostics only

Status: Accepted

## Context

A deployed service cannot address a developer's `localhost`. Publishing the backend through a general public tunnel increases exposure and makes tunnel/network behavior part of the measurement, producing misleading local performance results.

## Decision

Distribute `ScaleProof.Agent`, which creates an outbound authenticated control tunnel and executes k6 beside the local backend from a signed, expiring, non-replayable manifest. Upload only bounded redacted evidence. An optional HTTP relay tunnel is limited to low-volume reachability/correctness diagnostics and is never eligible for performance baselines or capacity claims.

## Consequences

The product maintains an additional signed executable, enrollment lifecycle, mTLS device identity, protocol compatibility, and local policy UX. The developer opens no inbound firewall port and target traffic normally stays local. Reports must identify execution location.

## Revisit when

A future isolated network cannot run an agent. Any alternative must preserve explicit authorization, target scoping, resource limits, evidence labeling, and measurement validity.
