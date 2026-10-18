# Error codes, termination reasons, and CLI exit codes

Status: Normative
Audience: backend implementers, frontend implementers, CLI and CI integrators
Related: FR-031, US-003, US-013, [API conventions](api-reference.md#conventions), [run lifecycle](run-lifecycle.md)

Codes are stable, `UPPER_SNAKE_CASE`, and never reused with a different meaning. Adding a code is a minor change. Removing one or changing its meaning is a breaking change.

## Problem Details codes (HTTP API)

| Code | HTTP | When | `errors[]` |
|---|---|---|---|
| `VALIDATION_FAILED` | 400 | Generic request validation (query, header, body shape) | yes |
| `SCENARIO_PARSE_ERROR` | 400 | YAML/JSON unparseable or > 256 KiB | line/column |
| `SCENARIO_SCHEMA_INVALID` | 400 | JSON Schema violation | JSON Pointer |
| `SCENARIO_UNKNOWN_FIELD` | 400 | Unknown property in v1 | JSON Pointer |
| `SCENARIO_SEMANTIC_INVALID` | 400 | Units, placeholders, unknown check, weights | JSON Pointer |
| `UNSUPPORTED_SCHEMA_VERSION` | 400 | `schemaVersion` not supported | — |
| `SECRET_LITERAL_DETECTED` | 400 | Secret-like literal in scenario | JSON Pointer (value never echoed) |
| `MUTATION_NOT_ALLOWED` | 422 | Initial-release non-read-only operation/mutation declaration, or missing future isolation/approval (Proposed ADR-022) | JSON Pointer |
| `TARGET_POLICY_DENIED` | 422 | Scheme, host, port, path, resolved IP, or redirect outside policy | `reason` ∈ `scheme`, `host`, `port`, `base_path`, `resolved_ip_private`, `resolved_ip_metadata`, `redirect_escape`, `dns_failure` |
| `LIMIT_EXCEEDS_POLICY` | 422 | Requested cap > effective cap | JSON Pointer + `limit` + `max` |
| `UNAUTHENTICATED` | 401 | No or expired session | — |
| `FORBIDDEN` | 403 | Authenticated but not authorized for the project or resource | — |
| `ANTIFORGERY_FAILED` | 403 | Missing or invalid CSRF token on a mutation | — |
| `RESOURCE_NOT_FOUND` | 404 | Also returned instead of 403 when revealing existence would leak data | — |
| `VERSION_CONFLICT` | 409 | `If-Match` mismatch | — |
| `IDEMPOTENCY_KEY_REUSED` | 409 | Same key with a different payload | — |
| `INVALID_STATE_TRANSITION` | 409 | E.g. baseline from an invalid run | — |
| `REPORT_NOT_READY` | 409 | Report requested before `completed`/terminal | — |
| `PAYLOAD_TOO_LARGE` | 413 | Body over limit | — |
| `QUOTA_EXCEEDED` | 429 | Durable project quota (queued/active/daily runs, artifact bytes, agents) | `quota`, `limit`, `current` |
| `RATE_LIMITED` | 429 | Middleware rate limit; with `Retry-After` | `policy` |
| `AGENT_UNAVAILABLE` | 409 | Selected agent offline or busy at submission | — |
| `AGENT_REVOKED` | 403 | Agent credential revoked | — |
| `AGENT_VERSION_UNSUPPORTED` | 409 | Agent below minimum version | `minVersion` |
| `ENROLLMENT_INVALID` | 400 | Enrollment code expired, used, or unknown (single code to avoid an oracle) | — |
| `DEPENDENCY_UNAVAILABLE` | 503 | PostgreSQL or artifact store unavailable | — |
| `INTERNAL_ERROR` | 500 | Unexpected; detail is generic, correlation ID present | — |

Problem `type` URIs are `https://scaleproof.dev/problems/<code-lowercase-kebab>`. The domain is a placeholder until a real one exists. `detail` never contains stack traces, filesystem paths, secrets, or raw input.

## Run termination reasons

`terminationReason` appears on every terminal run (see [run lifecycle](run-lifecycle.md)).

| Reason | Lifecycle | Meaning |
|---|---|---|
| `completed` | completed | Normal |
| `user_cancelled` | cancelled | Cancelled by a user or CLI |
| `abort_error_rate` / `abort_timeouts` / `abort_request_cap` / `abort_duration_cap` / `abort_generator_saturation` | cancelled | Automatic abort by a built-in safety rule |
| `abort_rule:<index>` | cancelled | A scenario `abort[]` rule tripped; `<index>` is its zero-based position. Together these are the `abort_*` family |
| `validation_failed` | failed | Worker/agent revalidation failed; no traffic sent |
| `engine_error` | failed | k6 failed to start or crashed |
| `lease_lost` | failed | Lease expired and no retry is permitted |
| `lease_expired_during_cancel` | cancelled | Cancel was not acknowledged in time |
| `reconciliation_required` | failed | Mutation attempt interrupted after traffic may have been sent |
| `queue_timeout` | failed | Not claimed within `Worker:MaxQueueAge` |
| `agent_unavailable` | failed | Agent offline beyond the dispatch timeout |
| `agent_rejected:<reason>` | failed | Agent rejected the manifest ([reasons](events-and-agent-protocol.md#message-types)) |
| `internal_error` | failed | Analysis/report failure; evidence retained |

## CLI exit codes

The CLI/agent executable (`scaleproof run|validate|compare|wait`; command consolidation Proposed ADR-022) maps outcomes as follows (FR-031):

| Exit | Meaning | Examples |
|---|---|---|
| `0` | Pass, or a non-blocking mode (`--mode warn` / `report`) | Thresholds passed; comparison pass |
| `1` | **Policy failure:** the application failed a declared threshold or regression gate | `outcome = fail`; comparison verdict `fail` |
| `2` | **Run not trustworthy:** `qualityStatus` is `inconclusive`/`invalid`, the comparison is `inconclusive`/`incompatible`/`not_comparable`, or the run was cancelled | Generator saturated; incompatible baseline |
| `3` | **Configuration or validation error:** the request was rejected before execution | Any 4xx except 429/5xx; `TARGET_POLICY_DENIED` |
| `4` | **Tool/infrastructure error** | Network failure to the API, 5xx, `engine_error`, `internal_error`, `lease_lost` |
| `5` | **Timed out waiting** (`--timeout`); the run continues under its existing execution authority | — |

Flags:

- `--mode gate|warn|report` (default `gate`)
- `--inconclusive fail|pass` (default `fail`, which yields exit `2`; `pass` maps exit `2` to `0` while still printing a warning) (US-013). `--inconclusive pass` applies only to inconclusive quality and comparisons. Cancelled, `invalid`, and `incompatible` results always exit `2` in `gate` mode

Artifacts (JSON and Markdown reports) are written before exiting for codes 0–2 whenever they exist.

## Plugin admission errors — Proposed (ADR-022)

| Code | HTTP | Meaning |
|---|---|---|
| `EXECUTION_LOCATION_UNSUPPORTED` | 422 | Reserved execution location not supported in this release |
| `ATTEMPT_AUTHORITY_EXPIRED` | 409 | Stale attempt/dispatch/lease or execution permit; cannot start, renew, or admit results |
| `AGENT_CLOCK_UNCERTAIN` | 409 | Agent/server clock uncertainty exceeds the configured safety allowance |

Agent protocol rejects use the same safe code names; `manifest.reject` additionally permits `clock_uncertain`. These do not create AI-related run termination reasons. Initial workload rejection remains `validation_failed` or `agent_rejected:<reason>` as appropriate.

## AI and draft errors — Proposed (ADR-023)

These codes appear in CLI errors, job `errorCode`, or Problem Details as applicable. Local-only failures are not exposed as browser credential diagnostics. Provider HTTP status/body is never copied to the API.

| Code | HTTP when applicable | Meaning |
|---|---|---|
| `AI_NOT_CONFIGURED` | 409 | Selected agent cannot perform AI work; manual execution is unaffected |
| `AI_PROVIDER_AUTH_FAILED` | — (job failure) | Local provider rejected credentials; safe diagnostic only |
| `AI_PROVIDER_RATE_LIMITED` | — (job failure) | Provider rejected capacity/budget; no automatic retry |
| `AI_PROVIDER_UNAVAILABLE` | — (job failure) | Network/5xx/incompatible provider response |
| `AI_REQUEST_TIMEOUT` | — (job failure) | Bounded provider deadline elapsed |
| `AI_OUTPUT_INVALID` | 400 or job failure | Model response/candidate/explanation violates its declared output contract |
| `AI_JOB_EXPIRED` | 409 or job failure | Local approval or work exceeded job lifetime |
| `AI_CONTEXT_NOT_APPROVED` | 409 | Input/destination/model hash lacks current local disclosure approval |
| `OPENAPI_IMPORT_INVALID` | 400 | Unsupported/malformed/oversized/deep/ambiguous contract or unresolved local reference |
| `OPENAPI_EXTERNAL_REFERENCE_DENIED` | 400 | Import contains an external reference; nothing is fetched |
| `DRAFT_NOT_READY` | 409 | Unresolved questions, incomplete data/SLO review, unsafe operation, or failed validation prevents publication |
| `EVIDENCE_REFERENCE_INVALID` | 400 | Explanation references missing, foreign, stale, or unapproved evidence |

Job cancellation uses state `cancelled`, not a run termination reason. CLI generation/import/publication exits `3` for configuration/validation/consent/provider-auth failures, `4` for provider availability/rate-limit/timeout or infrastructure failure, and `2` for cancellation. Safe command success exits `0`; these codes do not reinterpret any stored run outcome. No automatic inference retries occur for any code.
