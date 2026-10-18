# Metrics and statistics

Status: Normative. The histogram storage format is Proposed ([ADR-015](../decisions/ADR-015-hdr-histograms-for-latency.md)).
Audience: backend implementers, check authors, report consumers
Related: FR-013, FR-020, FR-021, FR-026, NFR-002, [ADR-003](../decisions/ADR-003-evidence-contract.md), [k6 adapter](k6-adapter.md), [comparison](comparison.md)

## Principles

1. **Missing is not zero.** An unavailable value is `null`, with an `unavailableReason`.
2. **Every value carries its context:** unit, definition ID, source, window, and sample count.
3. **Client and server timings stay apart.** Client-observed latency and server-span duration are separate metrics and are never mixed.
4. **Never average percentiles.** Percentiles are recomputed from mergeable histograms for exactly the requested window.
5. **Thresholds use one window.** Only the measurement window feeds thresholds and comparisons. Warm-up and ramp-down are recorded but excluded.

## Metric catalog

Names follow k6 where a k6 metric exists, so users recognize them. `definitionVersion` changes whenever the meaning changes.

| Metric | Unit | Type | Source | Definition | Valid aggregations |
|---|---|---|---|---|---|
| `http_reqs` | count | counter | k6 | HTTP requests completed (any status) in the window | `count`, `rate` (per s) |
| `http_req_failed` | ratio | rate | k6 | Fraction of requests that k6 marks failed (network error, timeout, or status outside `expected_response`, default 200–399) | `rate` |
| `http_req_timeouts` | count | counter | adapter | Requests ended by the request timeout | `count`, `rate` |
| `http_req_duration` | ms | trend | k6 | Client-observed `sending + waiting + receiving`, excluding connect/TLS. **Client latency** | `p50`, `p90`, `p95`, `p99`, `p99.9`, `max`, `min`, `mean` |
| `http_req_waiting` | ms | trend | k6 | Time to first byte | same as above |
| `checks_failed` | ratio | rate | k6 | Fraction of scenario assertions failed | `rate` |
| `iterations` | count | counter | k6 | Completed iterations | `count`, `rate` |
| `offered_rate` | iter/s | gauge | adapter | Configured arrival rate (open models) | `value` |
| `achieved_rate` | iter/s | derived | adapter | `iterations` started ÷ window duration | `value` |
| `dropped_iterations` | count | counter | k6 | Iterations not started because no VU was free | `count`, `ratio_of_offered` |
| `vus` | count | gauge | k6 | Active VUs | `max`, `mean` |
| `in_flight_requests` | count | derived | adapter | Requests in flight sampled per second (≈ VUs busy). **Observed concurrency** | `max`, `p95` |
| `data_sent` / `data_received` | bytes | counter | k6 | Bytes on the wire | `count`, `rate` |
| `generator_cpu` | ratio | gauge | agent (initial release, Proposed ADR-022) | CPU utilization of the k6 process tree, as a fraction of available cores | `max`, `p95` |
| `generator_memory` | bytes | gauge | agent (initial release, Proposed ADR-022) | RSS of the k6 process tree | `max` |
| `server_span_duration` | ms | trend | OTel | Server SERVER-kind span duration for the correlated run. **Server latency** (M1) | latency aggregations |
| `db_query_count` | count | derived | OTel | DB client spans per server trace (M1) | `p50`, `p95`, `max` |

Bounded dimensions: `request` (scenario request name), `status_class` (`2xx`…`5xx`, `error`), and `window` (`warmup`, `measurement`, `rampdown`). No other tags become stored dimensions (NFR-009).

## Percentile computation and sample adequacy

- **Storage:** per-interval HdrHistograms (10 s intervals, 3 significant digits, range 1 µs–10 min) per (`request`, `status_class`) ([ADR-015](../decisions/ADR-015-hdr-histograms-for-latency.md)). A percentile is computed by merging the histograms of the requested window, then querying `ValueAtPercentile`.
- **Adequacy:** a percentile *q* computed from *n* samples has `tail = floor(n × (1 − q))` observations above it. `PercentilePolicy v1` classifies it as follows:

| Tail samples | Status | Report behavior |
|---|---|---|
| ≥ 10 | `stable` | Shown; usable in thresholds and comparison |
| 1–9 | `unstable` | Shown with a warning; a threshold on it yields `inconclusive` |
| 0 | `suppressed` | `null` with `unavailableReason: insufficient_samples` |

  So p95 needs n ≥ 200 for `stable`, p99 needs n ≥ 1,000, and p99.9 needs n ≥ 10,000.
- `mean` is always reported alongside its sample count, but it is never the default threshold aggregation.

## Threshold evaluation

`ThresholdPolicy v1` is deterministic over stored evidence (FR-021):

1. Compute the aggregation over the measurement window. Apply the `request` scope if given.
2. If the value is `null`, or its percentile status is `unstable`, the threshold result is `inconclusive`.
3. Otherwise compare the value with the operator. Values are in canonical units, with no rounding before comparison.
4. Run `outcome`:
   - `fail` if any threshold fails
   - otherwise `warn` if a threshold is `inconclusive` and policy mode is `warn`
   - otherwise `pass`

   An `inconclusive` threshold in `gate` mode makes the run's `qualityStatus` `inconclusive` instead of producing an outcome.

## Generator validity and quality status

`QualityPolicy v1` (Proposed defaults, configurable per project policy):

| Signal | `inconclusive` when | `invalid` when |
|---|---|---|
| `dropped_iterations / offered` (open model) | > 1% | > 10% |
| `achieved_rate / offered_rate` | < 0.95 | < 0.80 |
| `generator_cpu` p95 | > 0.85 | — |
| Measurement window actually observed | < 90% of declared | < 50% of declared |
| Evidence completeness (all intervals present) | gaps ≤ 2 intervals | gaps > 2 intervals or attempt crashed mid-window |
| Required telemetry for a gated check | missing | — |
| Run cancelled | always (if any evidence) | — |

A run's `qualityStatus` is the worst of all signals. Each tripped signal is listed in the report under `quality.reasons[]` (FR-013).

## Interval evidence format

Each stored interval record contains:

- `attemptId`, `intervalStart`, `durationMs`, `window`, `request`, `statusClass`
- the counters
- a histogram reference: the artifact key plus offset, or an inline encoded histogram for small runs

This interval series is the input for [comparison statistics](comparison.md) and for charts.
