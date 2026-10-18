# k6 adapter

Status: Normative for the adapter contract; initial local-only execution and permit behavior are Proposed (ADR-022).
Audience: `ScaleProof.Execution` implementers, adapter authors
Related: FR-010–FR-013, [ADR-001](../decisions/ADR-001-use-k6.md), [ADR-020](../decisions/ADR-020-license-and-supply-chain.md), [backend architecture — shared execution boundary](../architecture/backend-architecture.md#shared-execution-boundary), [metrics](metrics-and-statistics.md)

The adapter turns a validated **execution plan** into a k6 process and turns k6 output into normalized evidence. It lives in `ScaleProof.Execution` and is invoked by the local agent in the initial release (Proposed ADR-022). The server worker orchestrates and analyzes; server-side invocation remains deferred.

## Engine interface (internal, not yet a public plugin contract)

```csharp
public interface IWorkloadEngine
{
    string EngineId { get; }                       // "k6"
    EngineCapabilities Capabilities { get; }       // supported profiles, version range
    ValueTask<EngineProbeResult> ProbeAsync(CancellationToken ct);   // version, path; used by readiness
    IAsyncEnumerable<EngineEvent> ExecuteAsync(ExecutionPlan plan, AttemptWorkspace ws, CancellationToken ct);
}
// EngineEvent = ProgressSample | IntervalEvidence | GeneratorSample | EngineDiagnostic | EngineExited
```

`ExecutionPlan` is derived only from the canonical scenario, the pinned target, and the effective policy (or a verified manifest). It contains resolved absolute URLs, effective caps, and secret **handles**.

## Script generation: no interpolation

- A **single static, versioned k6 script template** (`scaleproof-runner.js`, embedded resource) is shipped with the adapter. It reads its plan with `open('plan.json')` and builds k6 `options.scenarios` and request functions from data.
- Scenario text is never concatenated into JavaScript, so no scenario input can become code.
- `plan.json` is written into the attempt workspace with permissions `0600`.
- Secrets are injected at the last moment through environment variables named `SP_SECRET_<n>`, which the template reads with `__ENV`. Their values never appear in `plan.json`, logs, or arguments.

Profile mapping:

| Scenario profile | k6 executor(s) |
|---|---|
| `smoke` | `shared-iterations` |
| `fixed_vus` | `constant-vus` |
| `arrival_rate` | `constant-arrival-rate` |
| `steps` (M1) | `ramping-arrival-rate` with hold stages |

Windows are modeled as consecutive k6 scenarios: `warmup`, then `measurement` (`startTime` = warm-up duration), then `rampdown`. Each carries the tag `window:<name>`, so evidence is split without relying on clocks.

Abort rules are rendered as k6 thresholds with `abortOnFail: true` as a *secondary* defense. The local executor's own live evaluation is primary; server deterministic evaluation remains authoritative for final results (Proposed ADR-022).

## Process invocation

```text
k6 run --quiet --no-color --no-usage-report
       --out json=<ws>/samples.ndjson
       --summary-export=<ws>/summary.json
       --address 127.0.0.1:<port>         # REST API, loopback only; <port> = free port reserved by the agent
       <ws>/scaleproof-runner.js
```

- Use `ProcessStartInfo.ArgumentList`, an explicit absolute executable path, and the working directory set to the workspace.
- Build the environment from scratch: `PATH` (minimal), `HOME` = workspace, `K6_NO_USAGE_REPORT=true`, plus the `SP_SECRET_*` variables.
- Redirect stdout and stderr, capped at 1 MiB each, and redact them before storage.
- Enforce wall-clock limits from the outside: `warmUp + measurement + rampDown + gracefulStop + 30 s`.

## Ingestion

The adapter tails `samples.ndjson` incrementally with bounded reads:

1. It parses `Point` lines for the catalog metrics only, dropping any other metric names.
2. It routes each sample into the per-interval histograms and counters, keyed by its `window`, `request` (`name` tag), and status-class tags.
3. It emits `ProgressSample` events every second (≤ 2 events/s per attempt).
4. It counts requests itself and initiates the cancel path when `maxRequests` is reached. k6 has no native total-request cap.

## Cancellation

1. Send SIGINT on Unix (graceful stop; k6 stops new iterations and waits `gracefulStop`). On Windows, use the k6 REST API stop call (`PATCH /v1/status` with `{"data":{"attributes":{"stopped":true}}}`) on the port the agent passed in `--address`; verify this against the pinned k6 version.
2. If the process hasn't exited after `gracefulStop` + 5 s, kill the whole process tree (`Process.Kill(entireProcessTree: true)`).
3. Record `terminationReason` and keep everything ingested so far.

## Exit-code mapping

Maintain this table per supported k6 major version. It is golden-tested.

| k6 exit | Meaning | ScaleProof handling |
|---|---|---|
| 0 | Success | Normal |
| 99 | Thresholds failed | Ignored. ScaleProof evaluates thresholds itself; if an abort threshold tripped, map it to the matching `abort_*` reason |
| 104 | Invalid config | `engine_error`. This indicates an adapter bug, because plans are pre-validated |
| 105 | External abort (signal) | `user_cancelled` / `abort_*` as initiated |
| 107 / 108 | Script exception / script aborted | `engine_error` |
| other | Unknown | `engine_error`, with stderr excerpt (redacted) |

Codes must be verified against the pinned k6 release notes when bumping versions.

## Versioning

- Supported range: configured as `K6:SupportedVersions`, e.g. `>=1.0.0 <2.0.0`. `ProbeAsync` runs `k6 version` at readiness and rejects out-of-range binaries.
- Each report records the engine version, adapter version, and template hash.
- Golden fixtures: sanitized `samples.ndjson` excerpts for each supported minor version produce byte-identical normalized evidence ([testing strategy](../engineering/testing-strategy.md#golden-contracts)).

## Generator signals

While k6 runs, the agent samples the k6 process tree's CPU and RSS once per second. On Linux, read `/proc/<pid>/stat`; elsewhere use `Process` APIs. These samples feed `generator_cpu` and `generator_memory`, which drive [quality status](metrics-and-statistics.md#generator-validity-and-quality-status).

## Agent authority — Proposed (ADR-022, ADR-023)

Do not start until the local manifest checks and durable start journal commit succeed with a current worker-issued permit. Link execution cancellation to permit/deadline/control loss and local caps, with a monotonic clock. The process-stop deadline in Cancellation is included in the server's persisted retry stop bound. A restarted agent never replays an already journaled start.

Build k6/Collector environments from an allow-list; local AI keys and unrelated host secrets are excluded. Only explicitly referenced target `SP_SECRET_*` values reach k6. No inference output is interpolated into script/argv/paths. The agent reports engine status and normalized evidence; it cannot calculate an authoritative final outcome on the server's behalf.
