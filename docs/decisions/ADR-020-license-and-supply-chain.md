# ADR-020: Apache-2.0 license, k6 process boundary, and supply-chain controls

Status: Proposed
Date: 2026-10-17
Related: NFR-010, [release and versioning](../engineering/release-and-versioning.md), [ADR-001](ADR-001-use-k6.md)

## Context

Public release needs a license, and the k6 dependency is AGPL-3.0. The agent is a downloadable binary that verifies signed updates, so the release pipeline itself is part of the threat model.

## Options considered (license)

| Option | Pros | Cons |
|---|---|---|
| **Apache-2.0** | Permissive; explicit patent grant; enterprise-friendly; matches Gatling, OpenTelemetry, Kayenta, Otava | Allows closed forks/hosting |
| MIT | Simplest | No patent grant |
| AGPL-3.0 | Prevents closed hosted forks; matches k6 | Discourages enterprise contributors and embedding |

## Decision

1. **License: Apache-2.0** for all repository code and docs. Include a `NOTICE` file.
2. **k6 boundary.** ScaleProof never links, vendors, or modifies k6 source. It invokes an unmodified `k6` binary as a child process. The worker image and agent bundle may ship the official k6 release binary unmodified. In that case they include k6's AGPL-3.0 license text and a source pointer in `THIRD_PARTY_NOTICES`. If legal review prefers, users install k6 themselves and the agent verifies its version. Extensions built with `xk6` are out of scope.
3. **Supply chain:**
   - CycloneDX SBOMs for .NET (`CycloneDX` dotnet tool) and npm
   - Sigstore **cosign** keyless signing of container images and agent archives
   - SLSA provenance via GitHub's attestation action
   - Renovate for dependency updates, with grouped patch and minor updates
   - OpenSSF Scorecard workflow
   - Pinned GitHub Action SHAs
4. **Releases.** Use Conventional Commits with **Release Please** to produce `CHANGELOG.md` and SemVer tags.

## Consequences

- The agent self-update verifies cosign signatures against the expected OIDC identity (repo and workflow) before installing.
- `CONTRIBUTING.md` requires a DCO sign-off (`git commit -s`). No CLA.

## Revisit when

Legal review of AGPL binary redistribution says otherwise, or a hosted-competitor threat justifies a license change, which requires a new ADR and contributor consent.
