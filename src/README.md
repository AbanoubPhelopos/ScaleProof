# Source architecture

Status: Informative; source boundaries follow the Proposed ADR-022/023 target design.

The eight projects are scaffolding. The solution builds, but the API has no feature endpoints and the agent/worker hosted services only wait for shutdown. No enrollment, command dispatch, load execution, persistence, inference, or deterministic analysis is implemented. The agent's assembly/apphost name is `scaleproof`; this does not make the planned CLI commands available yet.

| Project | Responsibility | Direct project dependencies |
|---|---|---|
| Domain | Entities, value objects, state rules | None |
| Application | Use cases, ports, deterministic checks/analysis | Domain |
| Contracts | Versioned wire DTOs and serialization | None |
| Execution | Local k6 adapter, policy enforcement, normalization/redaction | Contracts |
| Infrastructure | Server persistence, artifact storage, signing adapters | Application, Domain |
| Api | HTTP/authz, gateway, SSE, composition | Application, Contracts, Infrastructure |
| Worker | Server leases, agent orchestration, evidence analysis/finalization | Application, Contracts, Infrastructure |
| Agent | Single CLI/sidecar, local policy/k6, uploads, optional local AI | Contracts, Execution |

The server worker does not reference Execution or start k6. Application does not depend on wire DTOs. The agent has no reference to API, Application, Infrastructure, or server persistence. Infrastructure references in composition roots are for registration; future feature code must use application ports.

API development profiles bind to loopback and do not open a browser. Launching the empty API is not a secured remote deployment; the [remote-access gate](../docs/delivery/roadmap-and-quality.md) must pass before exposing it beyond loopback.

See the [implementation blueprint](../docs/architecture/implementation-blueprint.md) for boundaries/invariants, the [roadmap](../docs/delivery/roadmap-and-quality.md) for vertical slices, and [local development](../docs/engineering/local-development.md) for planned workflows. ADR-022's worker-role change and ADR-023's AI behavior remain Proposed; this scaffold alignment does not accept those ADRs or complete their runtime slices.
