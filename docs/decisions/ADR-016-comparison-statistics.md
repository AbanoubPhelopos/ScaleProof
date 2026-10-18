# ADR-016: Statistical method for baseline/candidate comparison

Status: Proposed
Date: 2026-10-17
Related: FR-022, FR-026, US-012, US-013, [ADR-006](ADR-006-compatible-comparisons-only.md), [comparison spec](../specs/comparison.md)

## Context

After the compatibility gate ([ADR-006](ADR-006-compatible-comparisons-only.md)), the comparator must decide whether a candidate regressed. The docs require deltas, minimum samples, and variance awareness, but no method. A fixed-percentage rule on single runs ("p95 +10% ⇒ fail") is the industry default, and it is the main source of noisy CI gates.

## Decision drivers

- Reproducible from stored evidence (NFR-002)
- Robust to non-normal, heavy-tailed latency
- Explains its uncertainty
- Cheap to compute

## Options considered

| Option | Pros | Cons |
|---|---|---|
| Fixed relative thresholds only | Simple, explainable | Ignores noise; flaky |
| Welch's t-test on means | Familiar | Latency is not normal; means hide tails |
| **Mann–Whitney U (as in Kayenta) + bootstrap CI on percentile deltas** | Nonparametric; tail-aware via percentile CI; widely used for canary analysis | Needs repetitions or interval sampling; computation must be seeded |
| E-Divisive change point (Apache Otava) | Excellent on history | Needs a series, not a pair; complementary |

## Decision

1. **Unit of observation.** Use per-interval (10 s) values from the measurement window of each repetition ([ADR-015](ADR-015-hdr-histograms-for-latency.md)). If fewer than the policy minimum are available (default: 6 intervals per run and 3 runs for `gate` mode), the result is `inconclusive`. Never `pass`.
2. **Per-metric test.** Run a two-sided Mann–Whitney U on the interval values for baseline vs candidate. In parallel, compute a **bootstrap 95% CI of the relative delta** of the configured percentile:
   - 2,000 resamples
   - deterministic seed derived from the comparison ID
   - repetition-level resampling when repetitions exist, interval-level otherwise
3. **Classification per metric.** The direction of "worse" is defined per metric.
   - `fail`: the CI lies entirely beyond the configured practical threshold in the worse direction, and U-test p < α (default 0.01).
   - `pass`: the CI lies entirely within ±threshold.
   - `warn`: the CI excludes zero in the worse direction but does not exceed the threshold.
   - `inconclusive`: anything else.
4. **Aggregate.** The comparison is `fail` if any gating metric fails. Otherwise it is `inconclusive` if any gating metric is inconclusive and the policy treats inconclusive as blocking. Otherwise `warn` or `pass`.
5. **History (M1+).** Optionally run E-Divisive change-point detection over a baseline series. This uses the Otava method, via an in-process port or by exporting CSV to Otava. Its results are `informational` findings, never gates, until validated.

All parameters (α, threshold, resamples, minimums) belong to a versioned `ComparisonPolicy`, recorded in the comparison result.

## Consequences

- Statistics live in `ScaleProof.Application` as pure functions with golden fixtures. No external statistics package is needed: Mann–Whitney U with normal approximation and tie correction, plus bootstrap, together come to under 200 lines. Use `MathNet.Numerics` only if the exact distribution is needed for small n.
- Reports show the CI and p-value next to the delta, with a plain-language explanation.

## Revisit when

Validation on recorded regressions shows that the false-positive or false-negative rate is unacceptable, or users need multi-metric correction (e.g. Holm–Bonferroni).
