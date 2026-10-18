# AI assistance

Status: Proposed (ADR-023); local plugin delivery depends on Proposed (ADR-022).
Audience: agent, API, frontend, and security implementers
Related: [scenario v1](scenario-v1.md), [configuration](configuration.md#local-ai-options--proposed-adr-023), [API](api-reference.md#ai-and-draft-resources--proposed-adr-023), [protocol](events-and-agent-protocol.md#ai-jobs--proposed-adr-023), FR-048–FR-051, NFR-012

## Authority and inputs

AI produces drafts and explanations. The server's deterministic validators/checks remain authoritative. No model tool calls, scripts, network discovery, arbitrary file reads, or auto-launch are supported.

Import a local OpenAPI 3.0.x or 3.1.x JSON/YAML file. Parse with depth/size/alias bounds; disable YAML custom tags and reject duplicate keys. Resolve document-local `$ref` values with cycle detection and depth limits; reject file, URL, and other external references. OpenAPI `servers`, callbacks, links, and security URLs never grant network authority. Fetching a remote contract requires a developer to download it separately initially.

The developer selects operation IDs (or normalized method/path identities when absent), confirms they are non-mutating, and supplies:

- An existing target-profile version, approved local rule, and workload goal/profile.
- Synthetic dataset references for identifiers/parameters and logical authentication references. Dataset contents and secret values are not model inputs.
- SLOs or an explicit request for suggested SLOs, which remain unapproved until review.

Build a sanitized operation catalog containing only selected paths/methods, parameter/schema shapes, approved descriptions, safe examples, and developer goals. Strip security values, sensitive examples, hostnames, and unselected operations. Retain relative path templates needed to identify requests. Descriptions are off by default until included in the disclosure preview. Missing fixture IDs, credentials, business semantics, or SLOs are questions/blockers rather than fabricated defaults.

## Local disclosure and provider boundary

Environment configuration is owned by [configuration](configuration.md#local-ai-options--proposed-adr-023). `BaseUrl` names the API prefix; append `/chat/completions` exactly once after removing a trailing slash. Send a JSON request with `model`, `messages`, `stream: false`, and `max_tokens` bounded by configuration. Use `Authorization: Bearer <local key>`. Require a text `choices[0].message.content` response containing exactly one JSON object. Tool-call-only responses, markdown fences, truncated output, and invalid JSON fail validation. Do not rely on provider-specific JSON mode or structured-output support. Providers that reject this minimal profile are unsupported until an adapter is specified.

Only the locally configured destination is usable. Require HTTPS except explicitly configured loopback HTTP for local inference. Disable redirects; remote messages and model content cannot override endpoint/model or supply headers. User-configured inference is a separate authorized network destination, never a workload target rule.

Show the exact sanitized input, endpoint, model, and estimated input size locally before the first request. Consent binds their hash and prompt version. Any input, endpoint, model, or prompt change requires fresh local approval. Dashboard AI requests may queue work but remain `awaiting_local_approval` until this happens. An approved CLI generation command uses the same preview/confirmation; CI requires an explicitly supplied approved input hash, never an implicit yes to new context.

Keep API keys only in the parent agent's environment/memory. Start k6 and the Collector with an allow-listed environment that excludes AI and unrelated credentials. Never capture provider response bodies in logs/errors; redact validated outputs before local storage or upload. No server/browser endpoint accepts AI credentials.

## Draft lifecycle and review

An AI job is `awaiting_local_approval`, `queued`, `running`, `completed`, `failed`, or `cancelled`. `queued` means local disclosure consent has been recorded; jobs are separate from run attempts. A draft revision is `needs_input`, `ready_for_review`, or `published`.

Model generation returns `{ "scenario": <scenario-v1 candidate>, "questions": [<string>], "assumptions": [<string>] }`. Bounds apply to all fields. Validate the candidate against scenario v1 and the selected catalog. Reject unknown operations, unresolved placeholders, secret literals, unsupported profiles, and policy violations. Invalid candidates and safe diagnostics may be retained as `needs_input`; they cannot be published. Redact or discard unsafe content before any persistence. Never silently repair a scenario into an executable one.

Publishing requires a Maintainer/Owner or an enrolled principal with explicit `scenarios:publish` scope to review the exact revision, assumptions, target, dataset/authentication references, operation confirmations, limits, and thresholds. `If-Match` binds approval to the current revision. The server revalidates, creates an immutable scenario version and provenance record atomically, and marks that revision published. Replaying the same publication key returns the same version; changing reviewed content creates a new revision and invalidates approval. Publication never creates a run. Run submission pins the resulting scenario and current approved target/policy versions and revalidates again.

Local AI results remain local until the developer explicitly publishes a redacted draft or explanation. A remote request includes permission to upload the reviewed result, but cannot bypass disclosure approval or output review on the agent. Reject stale, cancelled, duplicate-with-different-hash, foreign-agent, or foreign-project results.

## Evidence explanations

After a terminal run, a user can request an explanation over a pinned report artifact/hash, deterministic findings, selected metric summaries, and approved bounded telemetry summaries. Do not send raw SQL, request/response bodies, access tokens, full traces, or arbitrary logs. The local preview identifies the destination and exact evidence being disclosed.

The model returns `{ "observations": [{ "text": "…", "evidenceRefs": ["…"] }], "hypotheses": [{ "text": "…", "evidenceRefs": ["…"], "limitations": ["…"], "nextExperiments": ["…"] }] }`. Every reference must resolve to an item in the approved evidence bundle and the run's project/attempt. Reference existence does not prove the text correct; label all output AI-generated and validate relevance through evaluation fixtures. Unsupported/unreferenced claims fail validation. Recommendations describe follow-up work and never execute it.

Upload accepted output as `scaleproof.ai-explanation/v1`, separate from `scaleproof.report/v1`. Include job/run/attempt identity, source report hash, input hash, endpoint identity without credentials, model, prompt version, creation time, redaction version, and review actor/time. Re-analysis or a different model produces a new artifact; it never replaces a deterministic report. Missing telemetry must appear as a limitation, not a diagnosis. Render model text as escaped plain text, without HTML or automatically actionable links.

## Bounds, failures, and retention

Use [configuration budgets](configuration.md#local-ai-options--proposed-adr-023). One explicit job makes at most one provider request; no automatic retries, fallback provider, or repair loop. Users can submit a new job after failure. This bounds duplicate billing and keeps changed input under review. Cancellation aborts local HTTP where possible but cannot guarantee provider billing cancellation.

Use safe codes from the [error catalog](error-codes.md#ai-and-draft-errors--proposed-adr-023). Provider authentication, rate limits, timeout, incompatibility, or malformed output only fail the AI job. A completed measurement remains usable. Offline/revoked agents cannot receive new AI jobs; expire outstanding jobs after the configured job lifetime. An interrupted running AI job becomes failed, not automatically replayed.

Store hashes, sanitized output, safe diagnostics, and provenance. Do not persist raw prompts, raw contracts, or provider error bodies centrally. Local sanitized job buffers and unpublished outputs expire under the local AI retention policy. Delete unpublished central drafts/jobs under the AI retention policy; published provenance follows its scenario/report retention. Project deletion removes drafts and explanations with other project data; retain only content-free audit events according to audit policy.

## Acceptance scenarios

- No key: manual validate/run/report succeeds; generation reports `AI_NOT_CONFIGURED`.
- External refs, alias bombs, unsafe methods, invented operations, missing datasets, and unapproved SLOs cannot produce a published runnable scenario.
- Secret canaries in examples, descriptions, provider errors, child environments, and model output never enter persisted output or disclosure previews.
- Prompt injection cannot invoke tools, expand operation scope, change destinations, or start traffic.
- Edits invalidate review; publication retries create one version; publication sends zero workload requests.
- Timeout, cancellation, provider 401/429/5xx, truncated/oversized output, and agent reconnect do not trigger automatic inference retries or alter test outcomes.
- Foreign/stale evidence references fail; missing telemetry is explicit; AI text cannot overwrite deterministic findings or execute markup.
