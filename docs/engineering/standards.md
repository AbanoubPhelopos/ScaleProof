# Engineering standards

Status: Normative
Audience: all contributors and coding agents
Related: [AGENTS.md](../../AGENTS.md) · [testing strategy](testing-strategy.md)

These standards preserve the useful repository guidance from the former root agent file while keeping product documentation independent of a particular coding assistant.

## General approach

- Prefer the simplest correct, secure, observable, and testable design. Add layers, packages, services, caches, brokers, or plugins only for a concrete requirement.
- Treat public contracts, migrations, data, deployment behavior, and accepted ADRs as compatibility constraints.
- Inspect the actual solution, target framework, conventions, CI, tests, and nearby code before changing implementation.
- Make focused changes; preserve unrelated work and never claim a check passed unless it was run and observed.
- Keep secrets and production data out of code, examples, fixtures, logs, reports, and command output.

## .NET implementation

- Target .NET 10 LTS for the initial implementation and stay current on supported patches; major upgrades require an explicit compatibility change.
- Enable nullable reference analysis and resolve warnings at the contract rather than suppressing them broadly.
- Use explicit dependencies, cohesive methods/types, typed validated options, and correct DI lifetimes. Singleton services must be thread-safe and cannot capture scoped services.
- Use async I/O and propagate cancellation. Avoid `.Result`, `.Wait()`, request-scoped fire-and-forget work, and unbounded parallelism/queues.
- Use `IHttpClientFactory`, explicit timeouts, and bounded retries only for transient and safely retryable operations.
- Prefer built-in framework features and repository conventions before adding dependencies.

## Boundaries and HTTP

- Keep controllers/endpoints thin; business, policy, and lifecycle rules belong in testable application/domain code.
- Keep API DTOs distinct from persistence models where coupling would leak schema or authorization-sensitive fields.
- Preserve routes/contracts unless a versioned breaking change is intentional. Use consistent status codes and Problem Details-compatible errors.
- Validate and normalize untrusted input; never trust caller-provided ownership, limits, destinations, prices, or roles.
- Bound and paginate collections. Do not expose internals, secrets, stack traces, or sensitive telemetry.

## Persistence and concurrency

- Follow PostgreSQL/EF Core conventions, project only required fields, avoid N+1 access, and use database-side filtering/paging.
- Parameterize SQL. Use explicit SQL only when it improves critical lease/transition correctness and cover it with integration tests.
- Put business-atomic operations in clear transaction boundaries; do not pretend transactions span target HTTP, engines, or artifact stores.
- Enforce uniqueness, foreign keys, indexes, concurrency, and deletion rules in the database where correctness depends on them.
- Treat migrations as production-impacting and design expand/migrate/contract or recovery paths when data exists.

## Security, reliability, and observability

- Authenticate callers and separately authorize each project/target/run/artifact operation; deny cross-project access by default.
- Defend relevant boundaries against SSRF, injection, over-posting, path traversal, unsafe capture, insecure deserialization, and arbitrary execution.
- Use structured, non-duplicative logs with stable context; redact sensitive values and keep metric dimensions bounded.
- Map expected validation/conflict/not-found/authorization outcomes explicitly; centralize unexpected exception handling.
- Make background work cancellable, observable, duplicate-safe, bounded, and resilient to process loss.

## Verification and change reporting

- Add unit tests for deterministic rules; integration tests for HTTP, serialization, DI, persistence, auth, leases, and middleware; end-to-end tests for the critical experiment flow.
- Test invalid input, authorization, missing data, concurrency, idempotency, cancellation, redaction, and partial failure where relevant.
- Run the narrowest relevant test first, then broader validation. Report environment/pre-existing failures separately.
- Review diffs for accidental files, secrets, debug code, generated noise, and compatibility regressions; run `git diff --check`.
- Document configuration, schema/API behavior, migrations, operational impact, risks, assumptions, and commands actually verified.
