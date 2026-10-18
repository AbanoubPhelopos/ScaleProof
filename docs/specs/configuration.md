# Configuration reference

Status: Normative for existing limits; plugin/AI additions and earlier remote availability are Proposed (ADR-022, ADR-023). This page is the single home for default limits and quotas; other documents link here.
Audience: operators, backend implementers
Related: [deployment](../architecture/deployment-architecture.md), [security and operations](../architecture/security-and-operations.md), NFR-009, NFR-011

Configuration uses the standard ASP.NET Core providers: `appsettings.json`, then `appsettings.{Environment}.json`, then environment variables (`ScaleProof__Worker__Concurrency`), then the command line. Every section binds to a typed options class with `ValidateDataAnnotations()`/`ValidateOnStart()`, so a misconfiguration fails startup instead of failing a run.

Feature flags and configuration **cannot** bypass target safety, authorization, redaction, or hard workload caps.

## Sections

| Section | Key | Default | Used by | Notes |
|---|---|---|---|---|
| `ScaleProof` | `Mode` | `Local` | API, Worker | `Local` (loopback bind, local session) or `Hosted` (requires OIDC before remote access, Proposed ADR-022) |
| `ConnectionStrings` | `ScaleProof` | — | API, Worker | PostgreSQL (Npgsql) |
| `Artifacts` | `Provider` | `FileSystem` | API, Worker | `FileSystem` (dev only) or `S3` |
| | `Root` | `./artifacts` | | Must be outside web content |
| | `S3:Endpoint`, `S3:Bucket`, `S3:Region` | — | | S3-compatible: AWS S3, SeaweedFS, Ceph RGW, Garage, … |
| `Worker` | `Concurrency` | `1` | Worker | Concurrent attempts per worker process |
| | `PollInterval` | `2s` ± 20% jitter | | |
| | `LeaseDuration` | `30s` | | |
| | `HeartbeatInterval` | `10s` | | Must be < `LeaseDuration / 2` |
| | `SweepInterval` / `ExpiryGrace` | `10s` / `5s` | | |
| | `MaxAttempts` | `2` | | Read-only runs only |
| | `MaxQueueAge` | `1h` | | Then `queue_timeout` |
| | `WorkspaceRoot` | OS temp + `/scaleproof` | | Per-attempt directories, deleted after upload |
| `K6` | `ExecutablePath` | `k6` on `PATH` | Agent | Resolved to an absolute path at startup |
| | `SupportedVersions` | `>=1.0.0 <2.0.0` | | |
| | `GracefulStop` | `15s` | | |
| `Policy:HardCaps` | see the workload table below | | API, Worker, Agent | Global ceiling; projects can only lower it |
| `Sse` | `EventRetention`, `MaxLifetime`, `HeartbeatInterval` | `1h`, `30m`, `15s` | API | |
| `RateLimits` | see the table below | | API | |
| `Quotas:Defaults` | see the table below | | API | Per-project defaults copied into `project_quotas` |
| `Auth` (M0 remote stage, Proposed ADR-022) | `Oidc:Authority`, `Oidc:ClientId`, `Oidc:ClientSecret` (secret ref) | — | API | Authorization code + PKCE; cookie `HttpOnly; Secure; SameSite=Lax` |
| `Agent` (M0, Proposed ADR-022) | `DispatchTimeout`, `OfflineGrace`, `MinVersion`, `ManifestTtl` | `5m`, `30s`, —, `5m` | API/Gateway | |
| `Telemetry` | `Otlp:Endpoint` | unset (console only) | all | ScaleProof's own telemetry |
| `ForwardedHeaders` (Hosted) | `KnownProxies`, `KnownNetworks`, `ForwardLimit` | empty | API | Forwarded headers are ignored unless the proxy is listed |

## Default workload hard caps

These are product safety defaults, not capacity promises. Operators may lower them. Raising them requires an audited policy change.

| Control | Default |
|---|---:|
| Arrival rate | 100 iterations/s |
| VUs | 100 |
| Duration (all windows) | 10 min |
| Total requests | 60,000 |
| Request timeout | 60 s maximum (10 s default) |
| Request body | 64 KiB |
| Response bytes read | 1 MiB per response |
| Artifact size | 50 MiB per artifact |

## Default API rate limits (per instance, ASP.NET Core partitioned limiters)

| Policy | Partition | Default |
|---|---|---|
| Authenticated reads | actor + project | 120 requests/min token bucket; 20 concurrent |
| Commands (excluding submission) | actor + project | 30/min; no queue |
| Run submissions | project | 5/min and 20/hour |
| Agent enrollment creation | actor + project; client IP | 3/hour; 10/hour per IP |
| SSE connections | actor; project | 5; 20 concurrent |
| Progress emission | attempt | 2 events/s |
| Artifact transfer | agent/project | 2 concurrent uploads |

## Default durable quotas (PostgreSQL admission)

| Quota | Default |
|---|---:|
| Queued runs | 20 per project |
| Active runs | 2 per project; 1 per local agent |
| Daily runs | 200 per project |
| Stored artifacts | 5 GiB per project before retention cleanup |
| Agents | 10 per project |

In M0 (`Mode=Local`), rate limits use one local partition and quotas still apply.

## Local AI options — Proposed (ADR-023)

Agent environment variables use the same `ScaleProof__` prefix as server options. The server does not bind or expose the `Ai` section. Never put these values in frontend build variables. When AI is disabled or incompletely configured, publish `aiAvailable: false` and a safe reason; manual execution remains ready. Invalid configured safety bounds fail startup rather than relaxing protection.

| Option under `ScaleProof__Ai__` | Default | Meaning |
|---|---|---|
| `BaseUrl` | unset | Developer-approved API prefix; HTTPS, or explicit loopback HTTP; no redirects |
| `Model` | unset | Exact configured model identifier; no silent fallback |
| `ApiKey` | unset | Local secret; required for AI calls; never echoed, persisted, uploaded, or inherited by children |
| `RequestTimeout` | `60s` | Maximum provider call duration |
| `MaxInputBytes` | `64KiB` | UTF-8 sanitized prompt/messages budget |
| `MaxOutputBytes` | `256KiB` | Bounded response stream, including envelope |
| `MaxOutputTokens` | `4096` | Requested token ceiling; byte limit still independently enforced |
| `MaxConcurrentJobs` | `1` | Per agent; shares a bounded local queue |
| `MaxQueuedJobs` | `5` | Per project, durably admitted; no hidden unbounded backlog |
| `MaxDailyJobs` | `50` | Per project; reserved at job creation, including failed/expired jobs |
| `JobLifetime` | `30m` | Includes waiting for local approval; then job expires as failed |
| `LocalRetention` | `24h` | Sanitized unpublished buffers/results; no raw prompt archive |
| `DraftRetention` | `7d` | Unpublished central drafts and completed job metadata; published provenance follows scenario/report retention |
| `MaxRequestsPerJob` | `1` (fixed) | No automatic retry, fallback, or model-repair request |
| `MaxImportBytes` / `MaxImportDepth` | `1MiB` / `64` | OpenAPI document size and parse/reference expansion depth |
| `MaxSelectedOperations` | `50` | Selected catalog bound; matches scenario request ceiling |

Configuration absence disables AI; provider authentication errors fail the job, not startup or agent execution readiness. Actual billing follows provider policy; these limits bound requests/bytes, not a universal currency amount. Operators may lower budgets. Raising project limits cannot bypass local consent or hard input/output bounds.

## Agent fencing and capture options — Proposed (ADR-022)

| Option | Default | Meaning |
|---|---|---|
| `Agent:ServerUrl` / `Agent:ArtifactOrigins` | enrolled server URL / explicit approved origins | Local control-plane URL and allowed upload destinations; remote messages cannot expand them |
| `Agent:EnrollmentTtl` | `10m` | Single-use enrollment validity |
| `Agent:ExecutionHeartbeatInterval` | `5s` | Authenticated attempt liveness, independent of progress |
| `Agent:ExecutionHeartbeatMaxAge` | `15s` | Worker cannot renew an executing attempt using an older heartbeat |
| `Agent:MaxClockUncertainty` | `2s` | Refuse starts/permit renewals if server-time uncertainty exceeds this |
| `Agent:MaxJournalBytes` | `10MiB` | Durable dispatch/nonce journal; full journal rejects new starts, never evicts live fences |
| `Agent:JournalRetention` | `24h` | At least manifest expiry plus execution stop bound; never prune an active fence |
| `Telemetry:Capture:MaxBufferBytes` | `16MiB` | Agent receiver/Collector capture buffer; record dropped evidence |
| `Telemetry:Capture:MaxSpans` | `10000` | Per attempt; excess is dropped and coverage reported |
| `Telemetry:Capture:DrainTimeout` | `15s` | Bounded drain after the capture window |
| `Retention:Runs` / `Retention:Audit` | `30d` / `90d` | Expiry of unpinned terminal run data and content-free audit events |

`Agent:OfflineGrace` and `ManifestTtl` are defined in the sections table. Manifest TTL is the latest acceptance/start window; execution has a separate signed deadline. Retry eligibility uses the persisted last permit's stop bound, not a fixed delay. Enforce `ExecutionHeartbeatInterval < ExecutionHeartbeatMaxAge < Worker:LeaseDuration` and validate clock/stop budgets against the lifecycle contract. AI and Collector endpoints are excluded from workload target rules.

On expiry delete unpinned terminal runs and their artifacts together; protect active runs, retained baselines/comparisons, and referenced provenance. Project deletion first revokes agents, cancels jobs/runs, waits for execution stop fencing, then removes project content. Keep only content-free security audit events until audit expiry. Report asynchronous deletion progress; do not advertise instant secure erasure from backups. Backups follow the operator's documented recovery/expiry policy.
