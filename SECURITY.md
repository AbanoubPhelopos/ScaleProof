# Security policy

## Reporting a vulnerability

**Do not open a public issue for security problems.**

Report vulnerabilities privately through GitHub's **"Report a vulnerability"** button (private vulnerability reporting) on this repository. The fallback is: TODO, maintainer security email (to be set before the public release).

Please include the affected version or commit, reproduction steps, and the impact. We aim to acknowledge within 5 business days. This target becomes binding once maintainers are listed in [GOVERNANCE.md](GOVERNANCE.md).

## Scope

The following are in scope:

- target-policy bypass (SSRF, DNS rebinding, redirect escape)
- workload caps or quota bypass
- secret leakage
- cross-project access
- agent manifest forgery or replay
- supply-chain issues in released artifacts

See the [threat model](docs/architecture/threat-model.md).

The following are out of scope: load caused against targets that you are authorized to test within the configured caps, and issues that require a compromised host running the agent.

## Supported versions

There is no release yet. See [release and versioning](docs/engineering/release-and-versioning.md#supported-versions).
