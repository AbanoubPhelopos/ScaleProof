# Competitive analysis and positioning

Status: Informative
Audience: maintainers, contributors, evaluators
Last reviewed: 2026-10-17. License and product facts change; re-verify them against each project's repository before quoting them publicly. Entries marked † were not re-verified on the review date.

## Why this document exists

ScaleProof deliberately **reuses** mature open-source engines and statistical methods rather than competing with them. This document records:

- what already exists
- what we reuse
- where ScaleProof is different
- what we refuse to build

Any proposal that duplicates a row below must explain why reuse is insufficient.

## Landscape

### 1. Load-generation engines

| Project | License | Language / scripting | Strengths | Gap relative to ScaleProof's goals |
|---|---|---|---|---|
| **k6** (Grafana Labs) | AGPL-3.0 | Go engine, JS scripts | Efficient, open/closed workload models, thresholds, rich outputs, large ecosystem | No experiment identity, no baseline compatibility gate, no evidence/finding contract, no run history |
| Grafana Cloud k6 / k6 Studio | Commercial SaaS / AGPL-3.0 desktop† | k6 | Hosted execution, private load zones, recorder, Grafana dashboards | SaaS-centric; comparison is visual, not compatibility-gated |
| **Gatling** | Apache-2.0 (Community); Enterprise is commercial | Java/Kotlin/Scala/JS DSL | High-performance, good HTML reports | Same gaps; enterprise features are paid |
| **Locust** | MIT | Python | Code-first, easy distribution | Closed model only; weak coordinated-omission handling |
| **Artillery** | MPL-2.0† | Node.js YAML/JS | YAML scenarios, Playwright integration, serverless runners | Same gaps; cloud features commercial |
| Apache JMeter / Taurus | Apache-2.0 | Java GUI / YAML wrapper | Protocol breadth, enterprise familiarity | Heavy; XML plans; weak diffability |
| NBomber | Commercial license for organizational use† | .NET | Native .NET, in-process | Licensing blocks redistribution |
| Vegeta | MIT | Go CLI | Exact constant-rate HTTP, HDR output | Single-endpoint focus; no orchestration |

**Decision:** keep k6 as the only M0–M2 engine ([ADR-001](../decisions/ADR-001-use-k6.md)). It has the best open-model support and structured output among permissively-consumable engines. Its AGPL license is satisfied by invoking the unmodified binary as a separate process ([ADR-020](../decisions/ADR-020-license-and-supply-chain.md)). The adapter boundary keeps Gatling or Vegeta possible later (US-041).

### 2. Orchestrators and CI runners

| Project | License | What it does | Relationship |
|---|---|---|---|
| k6-operator | Apache-2.0† | Distributes k6 tests across Kubernetes pods | Candidate executor for M3+ private agent pools; not needed before then |
| Testkube | Open-core† | Runs any test tool as Kubernetes CRDs | Orthogonal: ScaleProof could appear as a Testkube executor via the CLI |
| GitHub Actions + `grafana/setup-k6-action`† | MIT† | Runs k6 in CI | Our CLI adds compatibility, evidence, and exit-code semantics on top |

### 3. Continuous benchmarking and regression detection

| Project | License | Method | What we reuse |
|---|---|---|---|
| **Apache Otava** (incubating, formerly Hunter, from DataStax/MongoDB lineage) | Apache-2.0 | E-Divisive change-point detection over a time series of results (CSV/PostgreSQL/BigQuery/Graphite) | The algorithm family and the idea that history beats single pairwise comparison. Planned for baseline drift detection in M1+ ([ADR-016](../decisions/ADR-016-comparison-statistics.md)) |
| **Bencher** | MIT/Apache-2.0 core, Plus-licensed features | Stores benchmark results per branch/testbed with statistical thresholds | "Testbed" ≈ our environment fingerprint; thresholds-as-boundaries pattern |
| CodSpeed | SaaS; open-source runners† | CPU-instruction-count microbenchmarks | Not comparable (microbenchmarks, not services) |
| Nyrkiö | SaaS on Hunter/Otava† | Change detection as a service | Confirms demand for change-point over thresholds |
| Horreum (Red Hat) | Apache-2.0† | Performance result repository with schema-mapped "labels" and change detection | Versioned schema + label-extraction model resembles our report contract |

### 4. Canary and statistical analysis

| Project | License | Method | What we reuse |
|---|---|---|---|
| **Kayenta** (Netflix/Google, Spinnaker) | Apache-2.0 | Mann–Whitney U per metric, classified into pass/marginal/fail, with an aggregate score | The per-metric nonparametric classification and the explicit "nodata" handling. These map directly onto our `inconclusive` status |

### 5. Traffic capture and replay

| Project | License | Note |
|---|---|---|
| Speedscale | Commercial | Replays recorded production traffic with mocks. Different trust model, and out of scope for ScaleProof (privacy, mutation risk) |
| GoReplay | Open-core† | Shadowing/replay. Out of scope for the same reasons |

## Capability matrix

Legend: ✅ first-class · ◐ partial or needs glue · — absent · (Mx) is the ScaleProof milestone.

| Capability | k6 OSS | Grafana Cloud k6 | Gatling | Locust | Bencher | Otava | Kayenta | **ScaleProof** |
|---|---|---|---|---|---|---|---|---|
| Open-source, self-hostable | ✅ | — | ✅ | ✅ | ◐ | ✅ | ✅ | ✅ (Apache-2.0) |
| Open (arrival-rate) workload model | ✅ | ✅ | ✅ | — | n/a | n/a | n/a | ✅ via k6 (M0) |
| Immutable experiment identity (scenario/target/policy/engine hashes) | — | ◐ | — | — | ◐ | — | — | ✅ (M0) |
| Pre-traffic target safety policy (SSRF/redirect/DNS) | — | ◐ | — | — | n/a | n/a | n/a | ✅ (M0) |
| Generator-validity / quality status | ◐ | ◐ | — | — | — | — | ◐ | ✅ (M0) |
| Compatibility gate before regression verdict | — | — | — | — | ◐ | — | ◐ | ✅ (M1) |
| Statistical comparison (nonparametric) | — | ◐ | — | — | ✅ | ✅ | ✅ | ✅ (M1) |
| Change-point over history | — | — | — | — | ◐ | ✅ | — | ◐ (M1+) |
| OTel trace correlation / possible-N+1 finding | — | ◐ | — | — | — | — | — | ✅ (M1) |
| Correctness under retries (idempotency probes) | — | — | — | — | — | — | — | ✅ (M3) |
| Local-target execution from a hosted UI without inbound ports | ◐ (CLI) | ◐ (private zones) | — | — | n/a | n/a | n/a | Proposed agent-first M0 (ADR-022) |
| Evidence + confidence + limitations on every finding | — | — | — | — | — | — | ◐ | ✅ (M0) |
| Stable CI exit codes + JSON/Markdown reports | ✅ | ✅ | ◐ | ◐ | ✅ | ◐ | ◐ | ✅ (M0) |

## Positioning

> **ScaleProof turns a load test into an experiment:** immutable inputs, pre-traffic safety, a compatibility-gated comparison, and findings that carry evidence and admit uncertainty. It is built on k6 and OpenTelemetry rather than replacing them.

### Where we differ

1. **Comparability first.** Competitors compare any two runs. ScaleProof refuses or qualifies the comparison before computing deltas ([ADR-006](../decisions/ADR-006-compatible-comparisons-only.md)).
2. **Honest outputs.** `inconclusive` is a first-class result, and missing data is never zero ([ADR-003](../decisions/ADR-003-evidence-contract.md)).
3. **Correctness under load.** Idempotency and duplicate-side-effect probes have no open-source equivalent.
4. **Local-first, safe remote.** A deployed UI drives a local agent, so the measurement is not distorted by a relay ([ADR-012](../decisions/ADR-012-local-agent-tunnel.md)).

### What we refuse to build

- A new load generator (use k6, or later another engine through an adapter)
- A metrics/trace backend (use OpenTelemetry and existing backends)
- A recorder/HAR converter (k6 Studio exists)
- A Kubernetes test operator (k6-operator and Testkube exist)
- A SaaS-only feature set (all core features are self-hostable under Apache-2.0)

### Risks from the landscape

- **Grafana Cloud k6 could add compatibility gating.** Mitigation: ScaleProof stays engine-agnostic and self-hosted, and its evidence contract is an open schema.
- **k6 license or output-format changes.** Mitigation: the adapter boundary pins k6 version ranges and uses golden-fixture contract tests ([k6 adapter spec](../specs/k6-adapter.md)).
- **Market assumption unvalidated.** The PRD already requires user interviews before claiming demand.
