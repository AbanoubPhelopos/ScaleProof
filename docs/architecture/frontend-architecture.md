# Frontend architecture

Status: Proposed (ADR-022, ADR-023); existing accessibility and generated-client constraints remain binding.
Audience: frontend implementers, coding agents
Related: [API reference](../specs/api-reference.md) · [SSE events](../specs/events-and-agent-protocol.md#sse-run-events) · [ADR-010](../decisions/ADR-010-openapi-generated-client.md) · [ADR-019 (Proposed)](../decisions/ADR-019-frontend-codegen.md)

## Boundary

`web/scaleproof-web` is a React 19 TypeScript SPA for configuring experiments, observing execution, investigating evidence, and comparing runs. It is not a second policy engine; server validation and decisions are authoritative.

## Structure

```text
src/
├── app/                 # router, providers, query client, error boundary
├── api/
│   ├── generated/       # never hand edited
│   ├── client.ts
│   ├── problem-details.ts
│   └── events.ts
├── features/
│   ├── projects/
│   ├── targets/
│   ├── scenarios/
│   ├── runs/
│   ├── comparisons/
│   ├── findings/
│   └── settings/
├── shared/              # components, charts, hooks, formatting, styles
├── test/
└── main.tsx
```

Features may depend on `shared` and `api`, not another feature's private files. Route pages compose cross-feature behavior.

## Routes

| Route | Purpose |
|---|---|
| `/` | Last project or onboarding |
| `/projects/new` | Create project |
| `/p/:projectId/overview` | Recent runs, baselines, quick actions |
| `/p/:projectId/targets` | Target profiles and policy summary |
| `/p/:projectId/scenarios` | Versions, validation, launch |
| `/p/:projectId/runs` | Paginated/filterable history |
| `/p/:projectId/runs/:runId` | Live status, metrics, findings, artifacts |
| `/p/:projectId/compare` | M1 baseline/candidate comparison |
| `/p/:projectId/agents` | Enrollment, health, scopes, and locally approved rules |
| `/p/:projectId/scenario-drafts/:draftId` | Exact revision, assumptions/questions, validation and publication |
| `/p/:projectId/ai-jobs/:jobId` | Local-consent waiting, progress, result or safe failure |
| `/p/:projectId/checks` | Check capabilities and schemas |
| `/p/:projectId/settings` | Retention/project settings |

Project/resource identity, filters, tabs, baseline/candidate IDs, and shareable metric selections live in URL path/search state.

## State ownership

- TanStack Query owns server state, caching, mutations, and invalidation.
- React Router owns shareable navigation/filter state.
- React Hook Form owns drafts; Zod supplies client feedback derived from published contracts.
- Component state owns dialogs/disclosure/ephemeral UI.
- Versioned local storage holds only non-sensitive display preferences.

Do not add Redux/Zustand without an ADR. Never store tokens, secrets, credentials, evidence, or authoritative run state in browser storage.

## Contracts

ASP.NET Core emits `openapi.json`; a pinned generator creates TypeScript types, functions, TanStack Query options, and Zod schemas in `api/generated`. The proposed generator is `@hey-api/openapi-ts` ([ADR-019](../decisions/ADR-019-frontend-codegen.md)). CI fails on generation drift. `api/client.ts` supplies same-origin URL, credentials, correlation and antiforgery headers, cancellation, and typed Problem Details. Features do not call `fetch` directly except through the SSE adapter.

Generated code is never edited. Client schemas may improve UX but cannot weaken server rules. Contract tests cover nullability, enums, Problem Details, timestamps, and pagination.

## Fetching and live behavior

- Query keys are factories scoped by project/resource.
- Lists are server-paginated; mutations invalidate the smallest affected keys.
- Retry safe GET failures only; never automatically retry commands or policy/validation/conflict failures.
- Run detail loads a durable snapshot and then SSE. Sequence updates the cache; terminal state closes the stream and refetches.
- Browser reconnect uses `Last-Event-ID`; repeated failure activates capped polling and a visible degraded-live message.

## UX requirements

Every route implements loading, empty, error, forbidden/not-found, stale/degraded, and success states. Run detail separately presents lifecycle, data quality, threshold outcome, and finding severity.

Charts include units, interval/sample context, accessible summaries, and a data-table alternative. Large evidence loads on demand and is paginated/virtualized. Scenario authoring uses a structured v1 form plus canonical YAML/JSON preview—not arbitrary scripts.

Use semantic HTML, CSS variables, CSS Modules, and accessible shared primitives. Prefer an unstyled accessible primitive library (e.g. React Aria Components or Radix Primitives) over hand-rolled dialogs, menus, and comboboxes; choosing one requires an ADR. Meet WCAG 2.2 AA for keyboard navigation, focus, labels, contrast, announcements, and reduced motion. Workflows remain usable at 320 CSS pixels.

## Testing

- Vitest: formatting, URL state, Problem Details, query keys, adapters.
- Testing Library + MSW: forms, route states, accessibility names, user behavior.
- Playwright: onboarding, target/scenario validation, run launch/cancel, live terminal transition, evidence, comparison, authorization, keyboard flow.
- Run automated accessibility checks (`@axe-core/playwright`, `vitest-axe`) plus manual keyboard/screen-reader review.
- CI runs `tsc --noEmit` separately because Vite transpiles but does not type-check TypeScript.

## Plugin workflow and diagnostics — Proposed (ADR-022, ADR-023)

Onboarding is project → enroll agent → locally approve target rule → choose manual scenario or local OpenAPI/AI draft → review/publish → separately submit run → inspect evidence. Show offline/busy/revoked/incompatible agent and missing local approval explicitly. The dashboard cannot approve local file access or AI disclosure on the developer's behalf; pending jobs explain that action is needed in the local executable.

Draft review shows the exact revision, selected operations/non-mutation confirmation, dataset/auth references, questions, assumptions, SLO suggestions/confirmation, and effective limits. Editing invalidates review. A successful publication links to the scenario version and offers a distinct Run action, never auto-launches. Missing key/capability offers the manual scenario path without weakening safety.

Run detail presents endpoint latency/error/throughput tables and charts, offered versus achieved load, generator health, window/sample context, telemetry coverage, deterministic findings, and authorized evidence links. Database/dependency/resource views require relevant telemetry. Show missing, sampled, truncated, or time-only correlation as limitations. Resource attribution identifies the approved process/container; never conflate generator and target CPU.

Place AI explanations in an explicitly labeled optional panel after deterministic findings. Separate observations, hypotheses, limitations, and proposed follow-up experiments. Show source report version and stale-source context, with a new request action rather than overwriting old output. Render text escaped, without HTML, executable links, or command buttons. Explanation/job failure leaves reports available. Poll AI job state with bounded backoff and lifetime; do not extend terminal run SSE to await inference.

No browser field accepts an AI secret key. Do not copy keys into URLs, local storage, Vite variables, API DTOs, or telemetry. Regenerate client contracts only once the corresponding API implementation exists; generated files are not hand edited during this documentation revision.
