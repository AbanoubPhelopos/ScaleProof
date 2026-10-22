# Scenario contract v1

Status: Normative for the v1 shape; initial-release read-only admission is Proposed (ADR-022), AI provenance is Proposed (ADR-023). Canonicalization details are Proposed ([ADR-013](../decisions/ADR-013-rfc8785-canonicalization.md)).
Audience: backend/frontend implementers, scenario authors, coding agents
Machine-readable schema: [`schemas/scenario/v1.schema.json`](../../schemas/scenario/v1.schema.json)
Related: FR-001–FR-004, FR-010–FR-011, FR-021, [ADR-008](../decisions/ADR-008-versioned-immutable-contracts.md), [k6 adapter](k6-adapter.md), [error codes](error-codes.md)

A scenario describes **what** workload to send and **how** to judge it. It never contains executable code, shell fragments, absolute URLs, or secret values.

## Example

```yaml
schemaVersion: scaleproof.scenario/v1
name: catalog-read-baseline
description: Read-mostly catalog traffic at a fixed arrival rate.
target:
  profile: sample-api            # target profile name; the exact version is pinned at submission
  basePath: /api                 # optional; must be inside the profile's allowed base paths
workload:
  engine: k6
  profile: arrival_rate          # smoke | fixed_vus | arrival_rate | steps (M1)
  rate: 50                       # iterations per timeUnit
  timeUnit: 1s
  preAllocatedVUs: 20
  maxVUs: 60
  windows:
    warmUp: 30s
    measurement: 2m
    rampDown: 10s
  timeouts:
    request: 10s
    gracefulStop: 15s
  limits:                        # requested caps; effective = min(requested, policy)
    maxRequests: 20000
    maxRequestBytes: 64KiB
    maxResponseBytes: 1MiB
requests:
  - name: list-products
    weight: 80
    method: GET
    path: /products?page=1&size=20
    assertions:
      - status: [200]
      - header: { name: content-type, contains: application/json }
  - name: get-product
    weight: 20
    method: GET
    path: /products/{id}
    params:
      id: { dataset: product-ids, pick: round_robin }
    headers:
      Authorization: { secretRef: sample-api-token, format: "Bearer {value}" }
    assertions:
      - status: [200, 404]
thresholds:
  - metric: http_req_duration
    aggregation: p95
    operator: "<"
    value: 300
    unit: ms
  - metric: http_req_failed
    aggregation: rate
    operator: "<"
    value: 0.01
    unit: ratio
    request: list-products       # optional scope
abort:
  - metric: http_req_failed
    aggregation: rate
    operator: ">"
    value: 0.5
    unit: ratio
    over: 10s
checks:
  - id: scaleproof.repeated-query
    version: 1
    config: { minRepetitions: 5 }
classification:
  environment: local             # local | test | staging   (production is rejected in v1)
  mutation: false
capture:
  mode: metadata_only            # metadata_only | allow_listed (M2)
report:
  formats: [json, markdown]
```

## Field reference

| Path | Type | Required | Rules |
|---|---|---|---|
| `schemaVersion` | const string | yes | Exactly `scaleproof.scenario/v1`. Any other value gives `UNSUPPORTED_SCHEMA_VERSION`. |
| `name` | slug | yes | `^[a-z0-9][a-z0-9-]{1,62}$`; unique per project. Versions are numbered by the server. |
| `description` | string | no | ≤ 500 chars; excluded from the hash. |
| `target.profile` | slug | yes | Must name an existing target profile in the project. |
| `target.basePath` | path | no | Must start with `/`. Normalized (no `..`, `//`, or encoded traversal) and contained in an allowed base path. |
| `workload.engine` | enum | yes | `k6` only in v1. |
| `workload.profile` | enum | yes | `smoke`: required `iterations` (≤ 100), optional `vus` (≤ 5; semantic limit). `fixed_vus`: `vus`. `arrival_rate`: `rate`, `timeUnit`, `preAllocatedVUs`, `maxVUs`. `steps` (M1): `stages[]` of `{rate, duration}`. |
| `workload.windows.*` | duration | `measurement` required | `warmUp` and `rampDown` default to `0s`. Only `measurement` feeds thresholds (FR-011). |
| `workload.timeouts.request` | duration | no | Default `10s`, max `60s`. |
| `workload.limits.*` | integer / size | no | Requests above the policy cap give `LIMIT_EXCEEDS_POLICY`. Caps are never silently clamped. |
| `requests[]` | array, 1–50 | yes | `name` unique slug. `weight` 1–1000. `method` ∈ GET, HEAD, OPTIONS, POST, PUT, PATCH, DELETE (Proposed ADR-022: initial-release admission accepts only explicitly confirmed non-mutating GET/HEAD/OPTIONS operations with `classification.mutation: false`; all other methods or mutation declarations give `MUTATION_NOT_ALLOWED`. Reserved write methods require FR-005 isolation/approval in M3 before execution is enabled; the schema alone does not grant permission). `path` is relative: it starts with `/`, has no scheme or host, and its `{placeholders}` must be resolved by `params`. |
| `requests[].headers` | map | no | Values are literal strings or `{secretRef, format}`. Literal values matching the secret detectors (Authorization, Cookie, `*token*`, `*key*`, high-entropy strings ≥ 24 chars) give `SECRET_LITERAL_DETECTED`. Hop-by-hop headers and `Host` are forbidden. |
| `requests[].body` | object | no | `{ contentType, json }` or `{ contentType, dataset }`. Size ≤ effective `maxRequestBytes`. |
| `requests[].params` | map | no | `{dataset, pick: round_robin \| random_seeded}` or a literal. |
| `requests[].assertions[]` | array | no | `status: [int]`, `header: {name, equals \| contains}`, `jsonPath: {path, exists \| equals}`. They count into `checks_failed`, not `http_req_failed`. |
| `thresholds[]` | array | no | `metric` must be in the [metric catalog](metrics-and-statistics.md#metric-catalog). `aggregation` must be valid for that metric. `unit` must match its dimension. `operator` ∈ `<`, `<=`, `>`, `>=`. |
| `abort[]` | array | no | Same shape as a threshold, plus `over` (sliding window). Evaluated live; trips the `cancelling` path. |
| `checks[]` | array | no | `id` and `version` must be listed by `GET /api/v1/checks`. `config` is validated against that check's schema. |
| `classification.environment` | enum | yes | Must not be more permissive than the target profile's class. |
| `capture.mode` | enum | no | Default `metadata_only`. |
| `report.formats` | set | no | Default `[json, markdown]`. |

Unknown properties at any level give `SCENARIO_UNKNOWN_FIELD`, with the JSON Pointer of the field.

### Scalar syntaxes

- **Duration:** `^[0-9]+(ms|s|m|h)$`, a single unit only. `1m30s` is invalid, so use `90s`. Stored canonically in milliseconds.
- **Size:** `^[0-9]+(B|KiB|MiB)$`. Stored canonically in bytes.
- **Ratio:** a number in `[0, 1]`.

## Datasets and secrets

- **Datasets** (`dataset: product-ids`) are project resources: CSV or JSON arrays ≤ 1 MiB in M0, versioned and hashed like scenarios. The scenario hash includes the dataset hash.
- **Secrets** (`secretRef`) resolve inside the agent at execution time in the initial release (Proposed ADR-022), from explicitly mapped local environment variables or the agent credential store. AI receives reference names only. A secret value never enters a snapshot, log, report, or error (FR-004).

## Effective policy

At validation and submission:

```
effective = min(scenario.limits, targetProfile.policy, project.quota, global hard caps)
```

This is computed per field. A requested value above any cap is rejected, never clamped, so authors are not surprised. The resolved effective policy is stored as its own versioned, hashed `ExecutionPolicyVersion`.

## Canonicalization and hashing

Accepted (ADR-008): parse YAML/JSON into the typed model, apply documented defaults, sort unordered sets, preserve ordered arrays, and emit UTF-8 canonical JSON hashed with SHA-256. Store the canonical form and the canonicalization version.

Proposed ([ADR-013](../decisions/ADR-013-rfc8785-canonicalization.md)): the canonical JSON is produced with RFC 8785 JCS, and hashes are written as `sha256:<64 hex>` with `canonicalizationVersion: jcs-rfc8785/1`.

What is excluded from the hash:

- `description`
- comments and YAML formatting
- property order

What is included:

- every defaulted value
- referenced dataset hashes
- secret **reference names**, but never their values

## Validation order

Validation stops at the first failing stage. Each stage reports all of its own errors.

1. Parse YAML/JSON (size ≤ 256 KiB): `SCENARIO_PARSE_ERROR`
2. JSON Schema validation: `SCENARIO_SCHEMA_INVALID` / `SCENARIO_UNKNOWN_FIELD`
3. Semantic rules (units, placeholders, weights, checks exist): `SCENARIO_SEMANTIC_INVALID`
4. Secret-literal detection: `SECRET_LITERAL_DETECTED`
5. Target policy (scheme, host, port, resolved IP, base path, redirects): `TARGET_POLICY_DENIED`
6. Effective limits: `LIMIT_EXCEEDS_POLICY`

`POST /api/v1/validations` runs stages 1–6 and **sends no traffic to the target** (US-001). Stage 5 may perform DNS resolution only.

## Versioning

See [ADR-008](../decisions/ADR-008-versioned-immutable-contracts.md) and the contract versioning policy in [data and contracts](../architecture/data-and-contracts.md#contract-versioning-policy). Golden fixtures live in `tests/fixtures/scenario/v1/{valid,invalid}/`. Each invalid fixture names its expected error code and JSON Pointer.

## Imported and generated scenarios — Proposed (ADR-022, ADR-023)

Scenario v1 remains the engine-independent execution contract. Imported OpenAPI operations and AI text are candidate inputs, not executable authority. Only selected and explicitly confirmed non-mutating operations may publish/run. GET method semantics alone do not establish safety. Check placeholder and dataset/auth-reference completeness without reading secret values into snapshots or model context. Initial-release method/mutation restrictions are enforced on manual scenarios too, even where the schema reserves future write methods.

A published AI draft becomes an ordinary immutable scenario version through the review/publication API. Store AI input hash, model/prompt version, and approver in separate provenance records; do not add them as unknown scenario fields or alter the scenario hash semantics. Publish does not launch. See [AI assistance](ai-assistance.md) for disclosure and revision-bound review.

## .NET wire model

The implemented foundation model is
[`ScenarioV1`](../../src/ScaleProof.Contracts/Scenarios/ScenarioV1.cs), with
[`ScenarioJsonContext`](../../src/ScaleProof.Contracts/Scenarios/ScenarioJsonContext.cs)
serialization metadata. It retains optional input fields without resolving
execution defaults. JSON Schema validation precedes the DTO reader; reading a
DTO does not establish schema validity, target authorization, or admission to
execution. See [contract tests and reader boundaries](../../tests/README.md#scenario-v1-contracts-m0-p01-t03)
for fixture coverage and numeric representation limits.
