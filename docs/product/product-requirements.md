# Product requirements

Status: Proposed (ADR-022, ADR-023). Existing Accepted ADR invariants remain binding; this is the target design, not implemented behavior.
Audience: maintainers, contributors, product reviewers, coding agents
Related: [user stories](user-stories.md) · [competitive analysis](competitive-analysis.md) · [roadmap](../delivery/roadmap-and-quality.md) · [glossary](../glossary.md)

Requirement IDs (`FR-*`, `NFR-*`) are stable and never reused. The **Milestone** column says when a requirement must be met; see [Release scope](#release-scope).

## Vision and problem

ScaleProof makes the step between “this seems faster” and “this experiment proves what changed” practical for backend engineers. Today, performance investigations commonly compare mismatched workloads, over-rely on averages, ignore correctness or resource trade-offs, and lose the evidence needed to reproduce a conclusion.

ScaleProof tests backend services and backend protocols only. Its React application configures and visualizes experiments; it does not run browser, page-rendering, Core Web Vitals, or frontend UI tests.

The initial audience is individual backend engineers and small teams. This market assumption must be validated through user interviews; it is not an established business fact.

## Outcomes and success measures

| Outcome | Success signal (by end of M1 unless noted) |
|---|---|
| Local-first onboarding | A new contributor completes clone-to-report from documented commands in a clean environment. |
| Repeatable experiments | Every run records immutable scenario, target, policy, engine, build, and environment identity. |
| Safe execution | Disallowed targets and unsafe limits are rejected before traffic; cancellation and abort limits are tested. |
| Useful comparisons | Incompatible runs are rejected or explicitly qualified; absolute and relative deltas are reproducible. |
| Honest diagnostics | Every finding carries evidence, confidence, limitations, and a deterministic status. |
| CI usefulness | JSON and Markdown artifacts are produced with stable, documented exit codes. |

Product analytics should later measure time-to-first-valid-run, valid-run rate, comparison reuse, inconclusive causes, and repeat adoption. Do not collect them before privacy and retention behavior is documented.

## Personas

- **Backend developer:** needs fast local feedback after query, serialization, cache, or async changes.
- **Tech lead/staff engineer:** needs reproducible evidence, caveats, and trade-off visibility.
- **QA/performance engineer:** needs controlled workload shape, fixtures, thresholds, and exports.
- **SRE/platform engineer:** needs allow-lists, quotas, aborts, auditability, and operational signals.
- **Open-source contributor:** needs stable extension contracts, examples, and contract tests.

## Goals

1. Run bounded local HTTP smoke/load/concurrency experiments through k6.
2. Discover an observed capacity envelope under explicit workload and SLO conditions.
3. Compare a candidate with a compatible baseline.
4. Correlate available OpenTelemetry evidence and emit honest findings.
5. Draft reviewed scenarios from an API contract and explain evidence using optional local AI.
6. Export human- and machine-readable reports.
7. Permit extension without exposing arbitrary code execution by default.

## Non-goals (M0–M2)

- A custom load generator, hosted SaaS control plane, Kubernetes requirement, or full observability backend.
- Universal capacity claims, exact cloud billing, or correctness proofs without declared invariants.
- Arbitrary public-target stress testing, arbitrary shell/SQL execution, or automatic source modification.
- Support for every protocol, framework, database, and runtime.
- Browser automation, frontend rendering performance, visual regression, and end-user page testing.
- Autonomous AI testing, source-code scanning, traffic capture, write workflows, and AI verdicts.
- Server-originated target traffic or an in-process backend SDK in the initial release.

## Functional requirements

### Configuration and policy

| ID | Milestone | Requirement | Acceptance |
|---|---|---|---|
| FR-001 | M0 | Version named target profiles and scenarios. | A completed run references immutable target/scenario snapshots and hashes. |
| FR-002 | M0 | Validate targets and paths before execution. | Scheme, host, port, redirect, resolved IP, and base-path policy failures send no workload traffic. |
| FR-003 | M0 | Enforce trusted workload limits. | Duration, rate, concurrency, request count, payload, and artifact caps cannot be bypassed by scenario input. |
| FR-004 | M0 | Reference secrets rather than embed them. | Snapshots, logs, reports, and errors contain logical references or redacted values only. |
| FR-005 | M3 | Classify targets and gate mutations. | Mutation checks require an isolated non-production target and required approval policy. |

### Execution

| ID | Milestone | Requirement | Acceptance |
|---|---|---|---|
| FR-010 | M0 | Support bounded smoke, fixed-VU, and arrival-rate profiles through k6, reporting observed concurrency separately. | Reports distinguish offered load, achieved rate, iterations, requests, VUs, and observed concurrency where available. |
| FR-011 | M0 | Support warm-up, measurement, and ramp-down windows. | Only the declared measurement window drives thresholds; all windows are recorded. |
| FR-012 | M0 | Cancel and abort safely. | New work stops, in-flight work is timeout-bounded, partial evidence is retained, and a terminal state is reached. |
| FR-013 | M0 | Detect invalid generator behavior. | Dropped work or generator resource saturation can mark a run `inconclusive` or `invalid`. |
| FR-014 | M1 | Support step stress, spike, soak, burst, and capacity search. | Each profile has explicit semantics and reports only the tested envelope. |
| FR-015 | M3 | Execute through managed private-environment agent pools. | Fleet identity, scheduling, network policy, quotas, cancellation, and failure isolation are verified. |
| FR-016 | M0 | Execute against developer-local backends through an enrolled local agent. | The agent makes outbound-only control connections, validates signed jobs, runs the workload beside the target, and uploads bounded redacted evidence. |
| FR-017 | M3 | Offer a diagnostic HTTP tunnel for cloud-originated reachability checks. | Tunnel access is scoped to one approved target/session and is never used to claim local capacity or latency accuracy. |
| FR-018 | M0 | Collect opt-in local telemetry and target resource evidence. | A loopback collector or approved existing collector supplies bounded redacted run-correlated signals; process/container metrics require exact target approval. |

### Measurement, checks, and comparison

| ID | Milestone | Requirement | Acceptance |
|---|---|---|---|
| FR-020 | M0 | Normalize request, latency, error, timeout, and throughput metrics. | Values include units, definitions, source, interval, and sample count; unavailable is never represented as zero. |
| FR-021 | M0 | Evaluate configured thresholds deterministically. | The same stored evidence and policy version produce the same outcome. |
| FR-022 | M1 | Compare compatible baseline and candidate runs. | Compatibility status and reasons precede deltas; zero/missing baselines are handled explicitly. |
| FR-023 | M0 | Use a common finding contract. | Status, severity, confidence, summary, evidence, limitations, recommendations, check ID/version are present. |
| FR-024 | M0 | Detect repeated database-query patterns when spans exist. | Findings include fingerprints, trace scope, counts, aggregate duration, and appropriately qualified confidence. |
| FR-025 | M3 | Evaluate idempotency/replay invariants. | Sequential/concurrent replay uses a deterministic fixture, probe, assertion, observation window, and reset strategy. |
| FR-026 | M1 | Repeat experiments and expose variance. | Individual repetitions remain available and inadequate samples produce `inconclusive`. |

### Reports and lifecycle

| ID | Milestone | Requirement | Acceptance |
|---|---|---|---|
| FR-030 | M0 | Produce versioned JSON and Markdown reports. | A report can be reproduced from retained normalized evidence and identifies partial/redacted data. |
| FR-031 | M0 | Provide stable automation/CLI exit semantics. | The shared CLI/agent executable distinguishes pass, policy failure, invalid/inconclusive run, configuration error, and internal error. |
| FR-032 | M0 | Maintain a durable run lifecycle and attempts. | At most one active lease exists per run; retry attempts never mix samples. |
| FR-033 | M0 | Keep a safe audit trail. | Actor/mode, target, scenario, policy, override, and transition are recorded without secrets. |
| FR-034 | M0 | Apply retention and deletion policies. | Active artifacts are protected; project deletion semantics and audit retention are documented and tested. |

### Web application

| ID | Milestone | Requirement | Acceptance |
|---|---|---|---|
| FR-040 | M0 | Provide a React project workspace. | Stable URLs expose targets, scenarios, runs, checks, and settings; comparison routes arrive in M1. |
| FR-041 | M0 | Configure and validate through structured forms. | Client feedback preserves drafts; server validation remains authoritative and maps errors to fields/JSON paths. |
| FR-042 | M0 | Show live and durable run status. | SSE resumes by sequence, falls back to bounded polling, and terminal state triggers a persisted refetch. |
| FR-043 | M0 | Visualize metrics and evidence accessibly. | Charts include units/context and table alternatives; lifecycle, quality, threshold, and severity stay distinct. |
| FR-044 | M1 | Support history and comparison workflows. | Routes/search parameters make project, filters, baseline, and candidate linkable; lists are server-paginated. |
| FR-045 | M0 | Handle all route states. | Loading, empty, stale, degraded-live, validation, forbidden, not-found, conflict, and unexpected error states are tested. |
| FR-046 | M0 | Enroll and manage local agents. | Users can create a one-time enrollment, see agent health/capabilities, approve local target rules, revoke credentials, and select an online agent for a run. |

### Local plugin and AI assistance — Proposed (ADR-022, ADR-023)

| ID | Milestone | Requirement | Acceptance |
|---|---|---|---|
| FR-047 | M0 | Distribute one framework-independent CLI/sidecar. | `ScaleProof.Agent` supplies the `scaleproof` executable; CLI and dashboard submit through the same API, with local-only target traffic. |
| FR-048 | M0 | Import selected OpenAPI operations and developer context locally. | Bounded OpenAPI 3.0/3.1 parsing rejects external references; goals, datasets, authentication references, SLOs, and non-mutation confirmation are explicit. |
| FR-049 | M0 | Generate reviewed declarative scenario drafts using local AI. | Disclosure preview and consent precede inference; validation and revision-bound review precede publication; publication never starts traffic. |
| FR-050 | M0 | Offer evidence-linked AI explanations independently of measured outcomes. | Explanations reference retained evidence, identify hypotheses/limitations, and cannot change deterministic results. |
| FR-051 | M0 | Keep AI credentials and disclosure control local. | Environment key never leaves the agent credential boundary or reaches child processes; absent/failed AI does not prevent manual tests or reporting. |

## Non-functional requirements

| ID | Quality | Requirement |
|---|---|---|
| NFR-001 | Reliability | Run transitions, lease expiry, retries, duplicate dispatch, and cancellation races are tested. |
| NFR-002 | Correctness | Aggregation, comparison, and rendering are deterministic over immutable input. |
| NFR-003 | Security | Local control APIs default to loopback; remote/hosted mode is deny-by-default and tenant scoped. |
| NFR-004 | Privacy | Capture is metadata-only by default; redaction, size caps, and retention are versioned. |
| NFR-005 | Portability | The declared Docker environments pass a clean-clone smoke test. |
| NFR-006 | Observability | Logs, metrics, traces, and runbooks make stuck or invalid runs diagnosable. |
| NFR-007 | Extensibility | Public schemas/interfaces are versioned and backed by contract fixtures before third-party adoption. |
| NFR-008 | Accessibility | Any web report supports keyboard use, labels, and non-color-only status communication. |
| NFR-009 | Resource control | Queues, concurrency, payloads, artifacts, and result collections are bounded. |
| NFR-010 | Compatibility | Supported schema and engine version ranges, migrations, and deprecation rules are published. |
| NFR-011 | Abuse resistance | Edge limits, API rate limits, durable project quotas, queue admission, worker/agent concurrency, and workload caps prevent one actor from exhausting shared resources. |

| NFR-012 | AI isolation | Inference is bounded, explicitly authorized, non-executable, separately versioned, and tested against injection, disclosure, unsupported claims, and provider failure (Proposed ADR-023). |

## Release scope

Proposed (ADR-022, ADR-023): M0 is delivered in ordered stages, not as a server-execution MVP followed by a local-agent rewrite. The first useful release completes all M0 stages. Self-hosted means the team operates the control plane; it may still be remote from the developer machine. `Mode=Hosted` describes secured remote access, not a ScaleProof-operated SaaS offering.

| Milestone | Theme | Scope | Explicitly not included |
|---|---|---|---|
| **M0** | Plugin and evidence dashboard | Safe manual local run → secured remote dashboard/agent control → reviewed local AI drafts and explanations → optional correlated telemetry. Includes PostgreSQL leases, deterministic reports, endpoint metrics, read-only smoke/fixed-VU/arrival-rate workloads, enrollment, manifests, RBAC, quotas, retention, and sample backend. | Comparisons, write flows, server target traffic, SDK/IDE extensions, SaaS, tunnels, fleets |
| **M1** | Compare and gate | Compatible baselines, fixed-threshold comparisons, repetitions/variance, bounded step/capacity profiles, CI and comparison UI. Statistical method from Proposed ADR-016 only after acceptance. | Autonomous tests, write flows |
| **M2** | Operational maturity | Additional platform packaging, agent lifecycle/upgrade hardening, scale/recovery measurements, and deployment automation driven by demonstrated needs. | Automatic expansion of supported workloads |
| **M3+** | Advanced, separately specified | Mutation approval and isolated fixtures, dependent journeys, diagnostic tunnels, server execution, private pools, additional protocols, and isolated extension contracts. | Any feature without its own approved safety/evidence specification |

Every NFR applies as soon as its subject exists. Remote dashboard access is blocked until the M0 remote-security exit gate passes; a self-hosted deployment is not exempt. Resolve Proposed ADR-013/014/015/018 before the dependent runtime slices. Aspire and the frontend generator remain explicit decisions under ADR-017/019, not assumed prerequisites.

## Product principles

Measure before recommending; evidence before verdict; local first and remote by permission; preserve repeatability; make comparability explicit; evaluate correctness under load; fail safely; prefer a modular monolith before distribution; produce human and machine output; explain every heuristic's limitations.
