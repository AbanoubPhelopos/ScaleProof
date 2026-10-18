# ADR-015: HDR histograms as the latency evidence format

Status: Proposed
Date: 2026-10-17
Related: FR-020, FR-026, NFR-002, NFR-009, [metrics and statistics spec](../specs/metrics-and-statistics.md), [k6 adapter spec](../specs/k6-adapter.md)

## Context

The report contract requires latency percentiles with sample counts, warm-up exclusion, per-interval views, and reproducible comparisons. The docs never say how percentiles are computed or stored:

- **Percentiles cannot be averaged or merged.** Pre-aggregated p95 values per interval cannot give the p95 of the measurement window, or of several repetitions.
- **Raw per-request rows are unbounded.** Storing every request violates NFR-009 and the "do not persist every request as a row" rule.
- **k6's summary is too coarse.** The end-of-test summary covers the whole test including warm-up, uses fixed percentile selections, and can't be re-sliced.

## Decision drivers

- Mergeable across intervals and repetitions
- Bounded size
- Known error bound
- Deterministic re-computation from stored evidence

## Options considered

| Option | Pros | Cons |
|---|---|---|
| k6 end-of-test summary only | Zero work | No window slicing, no merge, includes warm-up |
| Raw samples (k6 `--out json`) persisted | Exact | Unbounded size; slow re-analysis |
| t-digest | Mergeable, compact, good tails | Approximation error is data-dependent; results vary with merge order |
| **HdrHistogram** | Mergeable; fixed relative error (configurable significant digits); deterministic; standard log format; used by wrk2/Vegeta | Fixed value range must be configured |

## Decision

- The local agent streams (initial release under Proposed ADR-022) k6 `--out json` (NDJSON) output **incrementally**. It discards samples outside the measurement window for threshold metrics, while keeping per-window counters, and records `http_req_duration` (and related timing breakdowns) into **per-interval HdrHistograms**.
  - Interval: 1 s for progress and 10 s for stored evidence.
  - Tags: bounded by request name and status class.
  - Histogram settings: 3 significant digits, 1 µs to 10 min.
- Store the histograms as a compressed artifact (HdrHistogram log format, versioned). `MetricSummary` rows hold derived percentiles plus `sampleCount`.
- Percentiles are always computed by merging histograms for the requested window. Do not average them.
- Library: **HdrHistogram.NET** (if unmaintained at implementation time, port the core recorder; the algorithm and log format are stable).
- The raw NDJSON is not retained beyond the attempt unless capture policy allows a bounded debug artifact.

## Consequences

- Reports must state the histogram precision as part of the latency definition.
- Golden fixtures: a sanitized k6 NDJSON stream produces the expected histograms and percentiles.
- Generator CPU for parsing NDJSON must itself be monitored. That is part of generator validity.

## Revisit when

k6 provides native mergeable histogram output (e.g. via an OpenTelemetry exponential-histogram output) that we can consume directly.
