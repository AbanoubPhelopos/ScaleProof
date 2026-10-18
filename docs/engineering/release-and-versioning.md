# Release and versioning

Status: Normative. Supply-chain tooling is Proposed ([ADR-020](../decisions/ADR-020-license-and-supply-chain.md)).
Audience: maintainers, release managers, integrators
Related: NFR-010, [contract versioning policy](../architecture/data-and-contracts.md#contract-versioning-policy)

## What is versioned, and how

| Artifact | Scheme | Compatibility promise |
|---|---|---|
| Product release (API, Worker, Web, Agent/CLI) | SemVer `MAJOR.MINOR.PATCH`. `0.x` until the M0 release gates and published compatibility fixtures pass (Proposed ADR-022) | Within a major: no breaking change to `/api/v1`, CLI flags/exit codes, or v1 schemas |
| HTTP API | Path version `/api/v1` | Additive only within v1. A breaking change means `/api/v2` running alongside v1 for at least one minor release |
| Scenario / report / check-result schemas | `scaleproof.<kind>/vN` | Output readers ignore additive fields; scenario input rejects unknown fields per scenario v1; breaking changes use `vN+1` and migration notes |
| Checks | `checkId` + integer `checkVersion` | Old versions remain selectable until deprecated |
| Agent protocol | `scaleproof.agent.vN` subprotocol | The server supports the current and previous agent minor releases |
| Agent binary | SemVer, released with the product | `hello.upgrade_required` below `Agent:MinVersion` |
| k6 | Supported range per release (`K6:SupportedVersions`) | Golden fixtures per supported minor |
| Database | Forward-only EF Core migrations | Upgrades from N-1 minor are tested in CI |

## Deprecation

1. Mark the item deprecated in the changelog and OpenAPI (`deprecated: true`), and emit a `Deprecation` response header with a `Sunset` date.
2. Keep it working for at least **one minor release, and at least 90 days**.
3. Remove it only in the next major release (or the next minor while in `0.x`, with a changelog entry under "Removed").

## Release process

1. Conventional Commits on `main`. **Release Please** maintains a release PR that updates `CHANGELOG.md` (Keep a Changelog format) and the version.
2. Merging the release PR tags `vX.Y.Z`. The release workflow then:
   - builds container images (API, Worker, Web) and agent archives (linux-x64/arm64, osx-arm64, win-x64; self-contained, single-file)
   - generates CycloneDX SBOMs
   - signs images and archives with **cosign** (keyless, GitHub OIDC) and attaches SLSA provenance
   - publishes a GitHub Release with checksums
3. The agent's self-update verifies the cosign signature identity (`https://github.com/<org>/scaleproof/.github/workflows/release.yml@refs/tags/v*`) before installing.

## Supported versions

| Version | Status | Security fixes until |
|---|---|---|
| — | No release yet | — |

The table is filled from real CI once the first release exists. Do not invent support windows before then.

## Plugin and AI release artifacts — Proposed (ADR-022, ADR-023)

Publish one signed `scaleproof` Agent/CLI archive per advertised platform, not separate CLI and agent products. Include compatible control protocol/schema/engine ranges and initial-release read-only restrictions. AI draft/explanation and protocol message schemas need golden fixtures before release; endpoint configuration names and the `chat-completions/1` profile are documented contracts, not a promise of every vendor's compatibility. No AI SDK/key is embedded in binaries or frontend assets.

Supply-chain implementation choices remain Proposed ADR-020; existing cosign/release-tool descriptions above are the proposed tooling profile, not verified CI. Resolve release signing/provenance before advertising downloadable binaries. Unsupported agents fail before execution rather than falling back to server traffic.
