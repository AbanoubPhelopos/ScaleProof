# Glossary

Status: Normative for existing terms; revised Agent/Worker roles and new AI terms are Proposed (ADR-022, ADR-023). Code, API fields, and UI labels use these terms with these meanings.
Audience: everyone

| Term | Definition | Not to be confused with |
|---|---|---|
| **Abort** | An automatic stop triggered by a rule (error rate, cap, saturation). It follows the `cancelling` path with reason `abort_*`. | *Cancel*, which is user-initiated |
| **Achieved rate** | Iterations actually started per second during a window. | *Offered rate* |
| **Agent** | `ScaleProof.Agent`, distributed as the `scaleproof` CLI/sidecar; initiates outbound control connections and runs k6 beside an approved backend (M0, Proposed ADR-022). | *Worker*, which is server-side |
| **Agent Gateway** | The control-plane module that terminates agent WebSocket sessions. | A tunnel or proxy |
| **Artifact** | An immutable, hashed blob produced by an attempt: histograms, report, bounded logs. | A *metric summary* row |
| **Attempt** | One execution of a run under one lease. Retries create new attempts, and their samples never mix. | *Run*, *repetition* |
| **Baseline** | A `valid` completed run nominated for comparison. | "Previous run" |
| **Capacity envelope** | The highest observed passing step under recorded conditions. Not a universal maximum. | "Max RPS" |
| **Check** | A pure, versioned evaluator that turns evidence into findings. | k6 `check()`, which is a *scenario assertion* |
| **Compatibility** | Whether two runs may be compared: `compatible`, `conditionally_comparable`, or `incompatible`. | *Verdict* |
| **Concurrency (observed)** | Requests in flight at the same moment. | VUs, connections, RPS |
| **Confidence** | The strength of evidence behind a finding: `low`, `medium`, `high`. Not a probability. | *Severity* |
| **Control plane** | The API, Agent Gateway, orchestrator/analyzer worker, PostgreSQL, and artifact store (Proposed ADR-022). It never sends target traffic. | *Data plane* (local agent → target initially) |
| **Diagnostic tunnel** | An optional relayed HTTP path for reachability checks only. Never valid as performance evidence (M3). | The agent control stream |
| **Effective policy** | The per-field minimum of scenario limits, target policy, project quota, and global hard caps. Versioned and hashed. | Scenario limits |
| **Environment fingerprint** | A hash of runtime, OS, architecture, CPU count, container limits, and agent identity, used by compatibility. | Build identity |
| **Evidence** | Normalized measurements and references that support a finding or verdict. | Raw engine output |
| **Execution location** | `server_worker`, `local_agent`, or `diagnostic_tunnel`. Fixed at submission. | Target environment class |
| **Finding** | The output of a check (status, severity, confidence, evidence, limitations). | *Outcome* |
| **Generator validity** | Whether the load generator delivered the intended load without saturating. | Target health |
| **Lease** | A time-bounded, tokened claim on an attempt. Only its owner can advance the attempt. | Lock |
| **Lifecycle** | Run execution state: `queued` … `completed`/`failed`/`cancelled`. | *Quality status*, *outcome* |
| **Manifest** | A signed, expiring, non-replayable instruction from control plane to agent; acceptance does not itself authorize engine start (Proposed ADR-022). | Scenario |
| **Measurement window** | The only window that feeds thresholds and comparisons. | Warm-up, ramp-down |
| **Mutation** | Any request that may change target state. Never auto-retried after traffic. | — |
| **Offered rate** | The configured arrival rate (open model). | *Achieved rate* |
| **Outcome** | The threshold/gate result of a valid run: `pass`, `fail`, `warn`. | *Lifecycle*, *quality status* |
| **Quality status** | Evidence trustworthiness: `valid`, `inconclusive`, `invalid`. | *Outcome* |
| **Repetition** | One of several runs of the same scenario submitted together to estimate variance (M1). | *Attempt* |
| **Scenario** | A versioned, declarative workload and judgment definition ([spec](specs/scenario-v1.md)). | k6 script |
| **Severity** | The potential operational impact if a finding is true. | *Confidence* |
| **Target profile** | A versioned, approved destination (origin, base paths, environment class, policy). | Local target rule (agent-side) |
| **Termination reason** | Why a run reached its terminal lifecycle ([catalog](specs/error-codes.md#run-termination-reasons)). | Problem Details code |
| **Threshold** | A scenario-declared bound on a metric aggregation in the measurement window. | Comparison threshold |
| **Verdict** | The aggregate comparison result: `pass`, `warn`, `fail`, `inconclusive`. | *Outcome* of a single run |
| **VU** | k6 virtual user, an executor of iterations. | Concurrency |
| **Worker** | `ScaleProof.Worker`, the server process that owns attempt leases, orchestrates agents, analyzes evidence, and finalizes reports; no initial target traffic (Proposed ADR-022). | *Agent* |

## Plugin and AI terms — Proposed (ADR-022, ADR-023)

| Term | Definition | Not to be confused with |
|---|---|---|
| **Plugin / sidecar** | The independently installed `scaleproof` executable beside the backend; command and service modes share one Agent project. | In-process SDK, IDE extension, arbitrary-code plugin host |
| **AI job** | Bounded inference workflow with explicit local disclosure consent and safe status. | Run, execution attempt |
| **Scenario draft** | Mutable reviewed candidate; publishing creates an immutable scenario version and sends no workload traffic. | Runnable approved version |
| **Disclosure consent** | Local approval of exact sanitized input, inference endpoint/model, and prompt version. | Scenario review or run approval |
| **Execution permit** | Worker-authorized bounded authority to execute one dispatch, no later than lease/deadline. | Socket connectivity or manifest acceptance |
| **Stop bound** | Persisted latest possible stop time for an outstanding permit, including process-stop/clock budget. Retry waits for it or a matching stop acknowledgment. | Database lease expiry |
| **AI explanation** | Separately versioned optional interpretation of pinned evidence; observations and hypotheses remain labeled. | Deterministic finding or proven root cause |
