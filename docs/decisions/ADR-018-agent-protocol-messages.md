# ADR-018: Agent control protocol — JSON Schema–typed messages over WebSocket

Status: Proposed
Date: 2026-10-17
Related: FR-016, FR-046, [ADR-012](ADR-012-local-agent-tunnel.md), [ADR-014](ADR-014-jws-signed-manifests.md), [agent protocol spec](../specs/events-and-agent-protocol.md)

## Context

`local-agent-and-tunnel.md` selects WebSocket "behind an interface" and lists message capabilities, but it defines no message schemas. An agent is distributed independently and may be one or two versions behind the server, so message compatibility is a public contract (NFR-010).

## Decision drivers

- Traverses corporate proxies on port 443
- Explicit versioning
- Generated validation in .NET
- Debuggable

## Options considered

| Option | Pros | Cons |
|---|---|---|
| **WebSocket + JSON messages with JSON Schema** | Proxy-friendly; human-readable; same schema tooling as scenarios/reports | Hand-written dispatch; larger frames |
| gRPC bidirectional streaming | Typed contracts, codegen, flow control, deadlines | HTTP/2 end-to-end is often broken by TLS-inspecting proxies; gRPC-Web doesn't support client streaming |
| SignalR | Reconnect helpers, .NET native | Hub semantics add an at-most-once layer we'd have to work around for sequence/ack; another protocol to version |
| HTTP/2 long-poll + POST | Simplest infra | Higher latency for cancellation; more requests |

## Decision

- Transport: WebSocket (`wss`, port 443, mTLS from M0 under Proposed ADR-022), subprotocol `scaleproof.agent.v1`.
- Every frame is a JSON envelope defined in `schemas/agent-protocol/v1.schema.json` (see the [spec](../specs/events-and-agent-protocol.md#agent-control-protocol)). Both sides validate it against the schema in debug and test builds, and with generated types in release builds.
- Sequence/ack/resume semantics live in the envelope, not the transport, so a future gRPC transport can carry identical messages.
- Proposed ADR-022 adds committed start/permit/execution-finish messages with durable stop fencing; Proposed ADR-023 adds inference jobs without workload authority. Resolve the expanded schema/fixtures before release. Release builds validate untrusted message bounds/shape as well as typed deserialization; debug-only schema checks are not a security boundary.

## Consequences

- Golden-fixture tests: N-1 agent messages are accepted by an N server, and vice versa where the compatibility window promises it.
- Use `System.Net.WebSockets` (`ClientWebSocket`, ASP.NET Core WebSockets). No third-party transport library.

## Revisit when

Measured message volume or cancellation latency is inadequate, or enterprise proxy telemetry shows HTTP/2 is reliably available (then gRPC becomes attractive).
