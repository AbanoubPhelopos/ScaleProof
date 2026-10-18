# Local development

Status: Informative; the plugin-first workflow is Proposed (ADR-022, ADR-023). Runtime commands below are planned, not verified: the repository contains scaffolding only.
Audience: contributors and early adopters
Related: [roadmap](../delivery/roadmap-and-quality.md), [configuration](../specs/configuration.md), [testing](testing-strategy.md)

## Current repository

The eight .NET projects are scaffolding. Compose orchestration, `scaleproof` packaging/commands, migrations, tests, and the React app still need implementation. The planned commands below specify the intended interface; they are not a working installation guide. Build the current solution with `dotnet build ScaleProof.sln`; report its result only after actually running it in your environment.

## Prerequisites for the target workflow

Use the pinned .NET SDK from `global.json`, Node/pnpm for the planned React workspace, Docker/Podman for PostgreSQL/sample services, and a compatible k6 binary on the agent host. AI is optional and needs a developer-configured compatible API endpoint, model, and local environment key.

## Planned manual workflow

```bash
# Planned: control plane, analyzer worker, PostgreSQL, web and disposable sample API.
docker compose up --build
# Planned: install the signed scaleproof archive for your advertised platform.
scaleproof enroll --server https://scaleproof.example.test --project sample
# Enrollment reads its one-time code interactively; do not put credentials in argv.
scaleproof target approve --origin http://127.0.0.1:5080 --base-path /api
scaleproof agent start
# In a second terminal, after creating/publishing a manual scenario:
scaleproof validate scenarios/catalog-read-smoke.yaml
scaleproof run --scenario catalog-read-smoke --version 1 --agent local
scaleproof wait --run RUN_ID
scaleproof report --run RUN_ID --format markdown
```

The initial local stage uses a loopback server/local session plus distinct enrolled agent identity. The remote URL above is illustrative and requires the roadmap's remote-security gate. Target approval asks for exact eligible operations/non-mutation confirmation and limits. Running inside containers requires an explicitly approved bridge/service origin; container loopback is not the developer host.

## Planned optional AI workflow

```bash
export ScaleProof__Ai__BaseUrl=https://inference.example.test/v1
export ScaleProof__Ai__Model=your-compatible-model
# Set ScaleProof__Ai__ApiKey through your shell's secret-input mechanism.
# Never paste a real key into command examples or commit it to a file.
scaleproof generate --openapi ./openapi.json --target sample-api
# Select operations/goals/fixture refs; locally preview context and approve disclosure.
# Review sanitized output before uploading a draft; confirm SLOs and publish separately.
scaleproof draft publish --draft DRAFT_ID --revision 1
scaleproof run --scenario catalog-read-smoke --version 2 --agent local
```

The environment must be available to the local command/service making AI requests; changing a shell does not change an already-running service's environment. Restart/reconfigure that local process explicitly. The dashboard never collects the key. No key is needed for manual tests. A dashboard-requested AI job waits for local disclosure/output review and cannot choose local file paths.

## Planned development and checks

Run API/Worker/Agent as separate processes; frontend development uses the configured loopback Vite origin. Compose is the clean-clone path. Aspire remains optional pending ADR-017; it is not a required project introduced by this revision. Generated frontend client files are regenerated from build-time OpenAPI only after endpoints exist.

Runtime verification will cover unit/architecture/contract tests, PostgreSQL and real-k6 integration tests, fake-provider AI tests, frontend typecheck/component/accessibility tests, and Playwright workflow coverage. See the testing strategy for exact acceptance cases. Do not advertise missing test projects or generated schemas as executable today.

## Conventions

- Keep local APIs on loopback and retain session/authz/antiforgery abstractions.
- Store target credentials locally as secret references. Keep AI credentials separate and exclude both from unrelated child environments.
- Runtime output belongs in ignored, permission-restricted workspaces/artifact roots outside web content.
- Sample fixtures use synthetic data and `.example.test` destinations; never commit real credentials, hosts, or personal data.
