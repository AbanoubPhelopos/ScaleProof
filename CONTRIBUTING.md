# Contributing to ScaleProof

Thank you for your interest. This project is in its design phase, and the [roadmap](docs/delivery/roadmap-and-quality.md) shows what is being built now (milestone M0).

## Before you start

1. Read [AGENTS.md](AGENTS.md) (documentation map and rules), the [glossary](docs/glossary.md), and the [engineering standards](docs/engineering/standards.md).
2. For anything beyond a small fix, open an issue first. For a design change, open a PR adding a **Proposed** ADR from the [template](docs/decisions/template.md).
3. AI-assisted contributions are welcome. Follow [AGENTS.md](AGENTS.md), and review every line you submit. You are responsible for it.

## Development

Setup and commands: [local development](docs/engineering/local-development.md). Testing expectations: [testing strategy](docs/engineering/testing-strategy.md).

## Pull requests

- Keep each PR focused: one vertical slice or one fix.
- Use [Conventional Commits](https://www.conventionalcommits.org/) titles (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`). Release notes are generated from them.
- Update the affected specs in the same PR when behavior changes.
- Include tests. A bug fix needs a test that fails without the fix.
- Fill in the PR template, including the commands you actually ran.
- Sign off your commits under the [Developer Certificate of Origin](https://developercertificate.org/) with `git commit -s`. There is no CLA.

## Licensing

By contributing, you agree that your contributions are licensed under the [Apache License 2.0](LICENSE).
