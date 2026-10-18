# ADR-013: RFC 8785 JSON Canonicalization for content hashes

Status: Proposed
Date: 2026-10-17
Related: FR-001, [ADR-008](ADR-008-versioned-immutable-contracts.md), [scenario spec](../specs/scenario-v1.md#canonicalization-and-hashing)

## Context

Scenario, target, policy, and manifest identity rely on a SHA-256 hash of a canonical form. `data-and-contracts.md` defines a bespoke canonicalization ("sort properties, preserve arrays, UTF-8"). A bespoke scheme leaves number formatting, Unicode escaping, and float edge cases undefined. It also forces every future reader (CLI, agent, third parties in other languages) to re-implement our rules exactly.

## Decision drivers

- Byte-identical output across .NET, TypeScript, and other languages
- A precisely specified number and string serialization
- Available, maintained implementations

## Options considered

| Option | Pros | Cons |
|---|---|---|
| Bespoke rules (current text) | Full control | Under-specified; every implementer re-derives edge cases |
| **RFC 8785 JCS** | IETF standard; ECMAScript number serialization; implementations in many languages; test vectors | Floats must be avoided or normalized for meaning (JCS defines bytes, not semantics) |
| Protobuf deterministic serialization | Compact | Explicitly not canonical across implementations or versions |
| CBOR deterministic encoding (RFC 8949 §4.2) | Binary, compact | Unfamiliar to contributors; scenarios are authored as YAML/JSON |

## Decision

1. Parse YAML/JSON into the typed v1 model.
2. Apply documented defaults.
3. Sort sets that are semantically unordered (e.g. `tags`, allowed methods). Arrays whose order matters, such as requests, stay as written.
4. Serialize to JSON.
5. Canonicalize with **RFC 8785 (JCS)** and hash the UTF-8 bytes with SHA-256.

Persist the hash as `sha256:<hex>` together with `canonicalizationVersion: "jcs-rfc8785/1"`.

Scenario values that need exact decimals (e.g. thresholds) are stored as JSON numbers within IEEE-754 safe precision, or as strings with a unit where they are not.

## Consequences

- .NET needs a JCS implementation. Use a maintained package (evaluate the `JsonCanonicalizer`/`Jcs` implementations on NuGet, cyberphone reference port) or a small in-repo implementation validated against the RFC 8785 test vectors.
- TypeScript uses the `canonicalize` npm package. Golden fixtures prove both produce identical hashes.
- Changing the default-application step changes hashes. That counts as a canonicalization version bump.

## Revisit when

A scenario needs data that JCS cannot represent losslessly, or hashing performance becomes measurable in admission latency.
