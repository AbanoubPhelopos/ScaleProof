# ADR-014: JWS (ES256) for signed execution manifests

Status: Proposed
Date: 2026-10-17
Related: FR-016, invariant 11, [ADR-012](ADR-012-local-agent-tunnel.md), [agent protocol](../specs/events-and-agent-protocol.md#execution-manifest)

## Context

The control plane signs an immutable execution manifest, and the agent verifies its signature, identity, expiry, and nonce. The current docs specify the fields but not the envelope or signature format. A home-grown signature envelope is a classic source of verification bugs: algorithm confusion, signing non-canonical bytes, and missing key IDs.

## Decision drivers

- Well-reviewed verification libraries in .NET
- Key rotation through a key ID
- Built-in expiry and replay claims
- No custom crypto code

## Options considered

| Option | Pros | Cons |
|---|---|---|
| Bespoke "hash + signature" fields | Simple to describe | We own canonicalization, algorithm agility, and verification bugs |
| **JWS compact (RFC 7515) with ES256** | `kid`, `exp`, `iat`, `jti` are standard; mature .NET support (`Microsoft.IdentityModel.JsonWebTokens`); small | JSON payload is not canonicalized (not needed: the signature covers exact bytes) |
| DSSE / in-toto envelope | Designed for supply-chain attestations; payload-type binding | Less common in .NET; no standard expiry/nonce claims |
| Rely on mTLS only | No extra signing | Doesn't protect against a compromised gateway or replay after the fact |

## Decision

- The manifest is a **JWS compact token, signed ES256** (P-256) by a control-plane key held in the platform key manager.
- Required header and claims:
  - `typ: "scaleproof-manifest+jwt"`
  - `kid`
  - `iss` (control-plane ID)
  - `aud` (agent ID)
  - `sub` (attempt ID)
  - `iat`, `nbf`, `exp`
  - `jti` (nonce)
  - `ver` (manifest schema version)
- The body fields are defined in the [agent protocol spec](../specs/events-and-agent-protocol.md#execution-manifest).
- The agent pins the accepted algorithm list to `ES256`, rejects `none`/HS*, verifies `aud` equals its own ID, enforces `exp` for launch with conservative clock uncertainty bounded by configuration, and durably retains seen `jti`/dispatch start fences through the maximum execution stop bound (Proposed ADR-022). Expiry of the acceptance window does not authorize replay or extend an execution permit.
- Public signing keys are delivered to agents as a JWKS over the authenticated control stream. Rotation keeps the old key valid until every manifest it signed has passed both its acceptance expiry and execution stop bound (Proposed ADR-022).

## Consequences

- Use `Microsoft.IdentityModel.JsonWebTokens` with explicit `TokenValidationParameters` (`ValidAlgorithms = ["ES256"]`). Do not use a generic JWT middleware.
- Tests: tampered payload, wrong `aud`, expired, replayed `jti`, unknown `kid`, `alg: none`, and algorithm confusion.

## Revisit when

Manifests must also serve as supply-chain attestations (consider DSSE), or post-quantum signature requirements arrive.
