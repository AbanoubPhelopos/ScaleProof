# User stories

Status: Proposed (ADR-022, ADR-023); requirement IDs retain their original meanings.
Audience: maintainers, contributors, coding agents
Related: [product requirements](product-requirements.md) · [glossary](../glossary.md)

Each story is independently testable. “Done” also requires the relevant security, observability, and documentation requirements.

## Epic A — First local experiment

### US-001 — Validate before traffic

Milestone: **M0**

As a backend developer, I want to validate a scenario and target without sending traffic so that configuration mistakes are safe.

Acceptance criteria:

- Invalid schema fields, units, limits, secret literals, target rules, and unsafe redirects are reported with actionable locations.
- Validation produces no target workload request.
- The effective policy and scenario hash are shown without secrets.

Links: FR-001–FR-004.

### US-002 — Run a bounded smoke test

Milestone: **M0**

As a backend developer, I want one command to run a local smoke test so that I can verify my API and test definition quickly.

Acceptance criteria:

- A clean local environment yields a terminal run and JSON/Markdown report.
- Request counts, status assertions, latency, errors, timeouts, duration, and engine version are present.
- Cancellation and configured caps remain enforceable throughout execution.

Links: FR-010–FR-013, FR-030–FR-032.

### US-003 — Understand an invalid run

Milestone: **M0**

As a developer, I want generator saturation and missing evidence separated from application failure so that I do not act on misleading results.

Acceptance criteria:

- Lifecycle status and quality status are separate.
- Offered versus achieved load and dropped work are visible.
- Unknown values are unavailable with reasons, never zero.

Links: FR-013, FR-020.

### US-004 — Navigate a project workspace

Milestone: **M0**

As a developer, I want one project-scoped workspace for targets, scenarios, runs, and checks (comparisons arrive in M1) so that context is never ambiguous.

Acceptance criteria:

- Project/resource identity is in the URL and survives refresh/deep linking.
- Empty projects show ordered onboarding rather than a blank dashboard.
- Forbidden and missing projects are handled without leaking existence.

Links: FR-040, FR-045.

### US-005 — Enroll a local execution agent

Milestone: **M0**

As a backend developer, I want to connect my machine to deployed ScaleProof without exposing localhost publicly so that I can test before deployment.

Acceptance criteria:

- Enrollment uses a short-lived one-time code and creates a revocable device identity.
- The agent initiates every control-plane connection; no inbound firewall port is required. Optional telemetry ingestion binds to authenticated loopback only.
- The UI shows online/offline, version, supported engines, last heartbeat, and approved local target rules without secrets.

Links: FR-016, FR-046.

### US-006 — Run against localhost safely

Milestone: **M0**

As a backend developer, I want the enrolled agent to run a workload against my local API so that measurements are not distorted by a public relay.

Acceptance criteria:

- The signed execution manifest identifies the agent, target rule, scenario/policy hashes, limits, expiry, and unique nonce.
- The agent independently validates the target, journals dispatch/start fencing, and runs k6 locally only with a current worker-issued execution permit.
- Only bounded redacted metrics/evidence/artifacts leave the machine.
- Disconnect, cancellation, lease loss, and attempted manifest replay reach deterministic outcomes.

Links: FR-002–FR-004, FR-012–FR-013, FR-016.

### US-007 — Diagnose through a temporary tunnel

Milestone: **M3**

As a developer, I want an explicitly enabled tunnel for a small reachability test so that ScaleProof can diagnose a local route when local execution is insufficient.

Acceptance criteria:

- The tunnel maps one opaque session to one approved local origin and expires automatically.
- It enforces method/path, connection, bandwidth, request, and duration limits at both ends.
- Reports label tunnel latency/throughput as non-capacity diagnostic data.

Links: FR-002–FR-003, FR-017.

### US-008 — Correlate local backend telemetry

Milestone: **M0**

As a backend developer, I want my local API to export run-correlated traces and metrics so that ScaleProof can detect database/dependency and resource regressions before deployment.

Acceptance criteria:

- The agent-managed collector binds to loopback with a generated bounded configuration, or the user selects an existing collector explicitly.
- Only the selected run window and approved telemetry fields are uploaded after redaction.
- Process/container resource metrics require exact target selection; unrelated processes, arguments, environment, and files are never collected.
- Missing telemetry makes dependent checks inconclusive/not applicable rather than passing.

Links: FR-018, FR-020, FR-024, NFR-004.

## Epic B — Capacity and regression

### US-010 — Discover a tested capacity envelope

Milestone: **M1**

As a performance engineer, I want controlled load steps evaluated against SLOs so that I can identify the highest observed passing step.

Acceptance criteria:

- The report records workload shape, environment, dataset/cache state, generator limits, thresholds, passing step, and first observed failing step.
- Abort criteria stop escalation when the target becomes unhealthy.
- The report never labels the observation a universal maximum.

Links: FR-011–FR-014, FR-020–FR-021.

### US-011 — Save an immutable baseline

Milestone: **M1**

As a developer, I want to nominate a valid run as a baseline so that later changes can be evaluated against known evidence.

Acceptance criteria:

- A baseline retains exact scenario, target, policy, engine, build, environment, and artifact identities.
- Invalid runs cannot silently become authoritative baselines.
- Retention impact is disclosed when a baseline is selected.

Links: FR-001, FR-020, FR-022, FR-034.

### US-012 — Compare a candidate fairly

Milestone: **M1**

As a tech lead, I want compatibility checked before metrics are compared so that environment drift is not presented as a code regression.

Acceptance criteria:

- Result is `compatible`, `conditionally_comparable`, or `incompatible`, with reasons.
- Absolute and relative deltas respect metric direction and zero/missing baselines.
- Minimum samples, variance, warm-up, and policy mode affect the conclusion.

Links: FR-021–FR-022, FR-026.

### US-013 — Gate CI without disguising noise

Milestone: **M1**

As a maintainer, I want explicit exit codes and warn-only/report-only modes so that CI is useful without becoming brittle.

Acceptance criteria:

- Artifacts are published even on threshold or quality failure when CI permits.
- Configuration/policy/tool failures differ from application threshold failures.
- Inconclusive results follow an explicit repository policy.

Links: FR-026, FR-030–FR-031.

### US-014 — Observe a run live

Milestone: **M0**

As a developer, I want a run page that updates while the workload executes so that I can observe progress and stop unsafe behavior.

Acceptance criteria:

- Lifecycle, quality, elapsed time, offered/achieved load, and cancel state are separate.
- Events resume after disconnect and polling fallback is visible.
- Terminal state refetches durable data; stale events cannot overwrite it.

Links: FR-012, FR-032, FR-042–FR-043.

### US-015 — Inspect a comparison visually

Milestone: **M1**

As a tech lead, I want a linked baseline/candidate view so that I can review deltas and compatibility evidence.

Acceptance criteria:

- Baseline/candidate IDs are linkable in the URL.
- Compatibility reasons precede metrics and cannot become a false verdict.
- Charts have units and table alternatives; deltas show value and direction.

Links: FR-022, FR-043–FR-044.

## Epic C — Evidence-backed diagnostics

### US-020 — Investigate repeated queries

Milestone: **M0**

As a backend developer, I want repeated DB spans grouped by request and fingerprint so that I can locate a possible N+1 pattern.

Acceptance criteria:

- The finding shows fingerprint, repetition count, aggregate duration, trace/span references, check version, and evidence limitations.
- Raw parameters and sensitive SQL content are not stored by default.
- Generic repetition is called “possible N+1” unless stronger framework evidence exists.

Links: FR-023–FR-024.

### US-021 — Verify sequential replay behavior

Milestone: **M3**

As a developer, I want to replay the same logical mutation and inspect its side effect so that I can test the endpoint's declared idempotency contract.

Acceptance criteria:

- The user declares request identity, expected responses, post-condition probe/assertion, observation window, and cleanup.
- A missing or failed probe yields `inconclusive` or `error`, never `pass`.
- Production-class targets are denied by default.

Links: FR-005, FR-023, FR-025.

### US-022 — Verify concurrent duplicates

Milestone: **M3**

As a developer, I want bounded concurrent same-key requests so that race-related duplicate side effects can be detected.

Acceptance criteria:

- Concurrency and request count are policy bounded.
- Final state is evaluated after a declared observation window.
- Same-key/different-payload behavior follows the scenario's declared API contract.

Links: FR-003, FR-005, FR-025.

### US-023 — Distinguish check failure from check error

Milestone: **M0**

As a report consumer, I want a consistent check contract so that I can tell an application finding from missing data or a broken evaluator.

Acceptance criteria:

- Status is one of `pass`, `fail`, `warn`, `inconclusive`, `not_applicable`, `skipped`, or `error`.
- Severity describes impact; confidence describes strength of evidence.
- `fail`, `warn`, `inconclusive`, and `error` require evidence or a limitation
  explaining why evidence is unavailable. `pass`, `not_applicable`, and `skipped`
  may have empty evidence and limitations, as defined by the
  [finding contract](../specs/finding-v1.md#field-semantics).

Links: FR-023.

### US-024 — Drill into finding evidence

Milestone: **M0**

As a developer, I want to inspect the evidence behind a finding so that I can verify its recommendation.

Acceptance criteria:

- Check version, severity, confidence, evidence, limitations, and recommendation are present.
- Large evidence is fetched/paginated on demand.
- Redacted or unavailable evidence includes a reason.

Links: FR-023–FR-024, FR-043.

## Epic D — Safe operation

### US-030 — Restrict target access

Milestone: **M0**

As a platform engineer, I want centrally and locally enforced target policy so that ScaleProof cannot become an SSRF pivot or arbitrary load generator.

Acceptance criteria:

- DNS results and every redirect hop are revalidated.
- Local and remote modes have separate private/loopback/link-local rules.
- Control-plane and metadata destinations are denied unless explicitly scoped.

Links: FR-002–FR-003, FR-015.

### US-031 — Stop a dangerous run

Milestone: **M0**

As an operator, I want manual and automatic aborts so that load stops when safety thresholds are crossed.

Acceptance criteria:

- New iterations stop first; current requests are bounded by timeout.
- Partial results and the abort reason are retained.
- Cancellation races resolve to one deterministic terminal state.

Links: FR-012, FR-032.

### US-032 — Recover from worker loss

Milestone: **M0**

As an operator, I want leases and heartbeats so that abandoned runs recover without mixing attempts.

Acceptance criteria:

- Only the valid server worker lease owner can finalize an attempt; agents cannot finalize authoritative outcomes.
- Each retry has a distinct attempt ID and artifact namespace.
- Started mutation tests are not automatically replayed without reconciliation/reset policy.

Links: FR-032.

### US-033 — Control sensitive artifacts

Milestone: **M0**

As a security reviewer, I want metadata-only capture and retention controls so that test evidence does not leak secrets or personal data.

Acceptance criteria:

- Known sensitive headers and seeded test secrets are redacted in automated tests.
- Capture allow-lists, size limits, redaction version, expiry, and project authorization apply to artifacts.
- Deletion does not remove active-run artifacts and has explicit audit behavior.

Links: FR-004, FR-033–FR-034.

### US-034 — Configure a safe target

Milestone: **M0**

As a platform engineer, I want the effective target and safety policy summarized before saving so that dangerous assumptions are visible.

Acceptance criteria:

- Environment, host/path, redirect policy, and hard caps are summarized.
- Secret references are never echoed as values.
- Server conflicts map to fields/paths without weakening policy.

Links: FR-001–FR-005, FR-041.

## Epic E — Contribution and extension

### US-040 — Add a deterministic check

Milestone: **M1**

As an open-source contributor, I want a versioned check contract and golden fixtures so that I can add a rule without learning the whole system.

Acceptance criteria:

- The check consumes a normalized evidence snapshot and cannot independently send traffic.
- Schema, status behavior, example fixture, and contract tests are documented.
- Breaking contract changes use a new version and migration/deprecation note.

Links: FR-023, NFR-007, NFR-010.

### US-041 — Add a workload-engine adapter

Milestone: **M3**

As a contributor, I want a bounded adapter interface so that another engine can normalize output without changing orchestration.

Acceptance criteria:

- Validation and execution honor cancellation and effective policy.
- Exact engine/adapter versions and original bounded output are retained.
- Contract tests cover offered/achieved load, failures, partial output, and cancellation.

Links: FR-010, FR-013, NFR-007.

### US-042 — Use the dashboard accessibly

Milestone: **M0**

As a keyboard or assistive-technology user, I want every experiment workflow operable without pointer or color dependence.

Acceptance criteria:

- Core routes pass automated accessibility checks and manual keyboard review.
- Focus, announcements, labels, contrast, reduced motion, and error summaries meet policy.
- Metric tables expose the information shown in charts.

Links: FR-043, FR-045, NFR-008.

## Journey traceability

| Original journey | Stories |
|---|---|
| Test a local API | US-001–US-008 |
| Discover capacity | US-010, US-014 |
| Compare baseline/candidate | US-011–US-015 |
| Detect possible N+1 | US-020, US-023–US-024 |
| Verify idempotency | US-021–US-023 |
| Run in CI | US-013 |

## Epic F — Local AI assistance — Proposed (ADR-023)

### US-043 — Draft scenarios for my API

Milestone: **M0**

As a backend developer, I want to select operations from a local OpenAPI contract and describe workload goals so that I can draft an appropriate scenario without writing a load script.

Acceptance criteria:

- Import supports bounded OpenAPI 3.0/3.1 JSON/YAML and rejects external references without fetching anything.
- I confirm selected operations are non-mutating and supply dataset/auth references; missing knowledge becomes questions.
- I see sanitized context, endpoint, and model before locally approving disclosure. My AI key remains in the local environment.
- The result is a declarative draft with assumptions, questions, and validation issues; it sends no workload traffic.

Links: FR-048–FR-049, FR-051, NFR-012.

### US-044 — Review before publishing or running

Milestone: **M0**

As a developer, I want to review the exact scenario revision, SLOs, and effective limits so that generated guesses cannot silently become tests.

Acceptance criteria:

- Editing invalidates approval; unresolved inputs and unsafe/invented operations block publication.
- Server revalidation and one idempotent publication transaction create one immutable scenario version.
- Publication never starts traffic; CLI and dashboard require a separate run submission selecting an online agent and approved local rule.

Links: FR-047, FR-049, FR-002–FR-004.

### US-045 — Explain evidence without changing results

Milestone: **M0**

As a backend developer, I want an optional explanation of retained evidence so that I can choose a useful follow-up experiment.

Acceptance criteria:

- Local disclosure/output review applies to a pinned report and selected sanitized evidence.
- AI observations and hypotheses reference evidence and show limitations; missing telemetry cannot become an asserted root cause.
- Explanation artifacts are versioned independently; no key, outage, malformed output, or AI cancellation changes a deterministic report/outcome.
- Manual validate/run/report works without AI configuration.

Links: FR-050–FR-051, NFR-012.

### US-046 — Know what the diagnosis can support

Milestone: **M0**

As a dashboard user, I want evidence coverage and limitations beside endpoint findings so that I can distinguish observed slowness from an inferred bottleneck.

Acceptance criteria:

- HTTP latency/error/load and generator health are available without backend instrumentation.
- Selected Collector pipelines explicitly export filtered telemetry to the local agent receiver; unsupported/missing correlation is labeled.
- Database/dependency findings reference relevant spans; resource observations name the approved process/container and sampling interval.
- Plain-text AI hypotheses remain separate from deterministic findings and cannot run recommendations.

Links: FR-018, FR-020, FR-023–FR-024, FR-043, FR-050.
