# AzureDevopsApi Knowledge Base

## What This App Is

AzureDevopsApi is a .NET 8 backend (`ADOApi`) plus a React/Vite frontend (`adoapi-ui`) for Azure DevOps workflows, documentation automation, and AI-assisted repository/security operations.

Primary capabilities in the current codebase:

- Azure DevOps work item and repository operations.
- AI-assisted querying/chat/document generation using Azure OpenAI or Ollama.
- Security advisor flows for SARIF/SBOM analysis and PR feedback.
- Entra-authenticated UI and API usage with role-based authorization in production.

## Repository Map

- `ADOApi.sln` - solution file.
- `ADOApi/` - ASP.NET Core Web API (main backend).
- `ADOApi.Tests/` - xUnit + Moq backend tests.
- `adoapi-ui/` - React 19 + TypeScript + Vite frontend.
- `.github/workflows/` - CI, secret scan, SBOM workflows.
- `README.md`, `ARCHITECTURE.md`, `DEPLOYMENT.md`, `SECURITY.md`, `DEV_GUIDE.md` - project docs.

## Backend (ADOApi) Index

### Runtime and Composition

- Entry point: `ADOApi/Program.cs`.
- Framework: ASP.NET Core on `.NET 8`.
- API versioning configured with URL segment versioning.
- Key middleware:
  - `GlobalExceptionHandler`
  - `AuditCorrelationMiddleware`
  - `RateLimitingMiddleware`
- Swagger UI enabled at app root route.

### Authentication and Authorization

- Development mode: permissive policies (all role checks effectively pass).
- Production mode:
  - JWT bearer via Microsoft Identity Web.
  - Fallback policy requires authenticated users.
  - Role policies: `ADO.ReadOnly`, `ADO.Contributor`, `ADO.Admin`.

### Data and Persistence

- EF Core `SecurityAdvisorDbContext` in `ADOApi/Data/`.
- Provider switch via config:
  - default `SQLite`
  - optional `SQLSERVER`
- DB is `EnsureCreated()` on startup.

### External Integrations

- Azure DevOps SDK (`Microsoft.TeamFoundationServer.Client` family).
- Azure Key Vault (optional, if `KeyVault:VaultUri` configured).
- LLM provider abstraction:
  - Azure OpenAI (`AzureOpenAiClient`)
  - Ollama (`OllamaClient`)
  - factory selected by `LLM:Provider`.

### Service Layer (Key Files)

- Azure DevOps and core ops:
  - `AzureDevOpsConnectionFactory`, `AzureDevOpsService`, `RepositoryService`, `WorkItemService`, `QueryService`
- AI and docs:
  - `SemanticChatService`, `DocsGenerationService`, `WorkItemProposalService`
- Security advisor and governance:
  - `SecurityAdvisorService`, `SecurityGovernanceService`, `SecurityAdvisorRepository`, `PullRequestCommentService`, `RiskAcceptanceExpiryService`
- Repo intelligence:
  - `RepoMemoryService`, `InsightService`, `WorkItemLinkService`, `AgentRunService`, `FingerprintService`
- Cross-cutting:
  - `CachingService`, `ResiliencePolicies`, `AuditLogger`, `WebhookService`

### Controllers and Route Surface

- `WorkItemController` - `/api/workitem/*`
  - work item CRUD, templates, filtering, relations, project/user queries.
- `RepositoryController` - `/api/repository/*`
  - repositories, content/directory, commits, branches, file create/update/delete, repo structure.
- `ProjectController` - `/api/project/*`
  - projects and iterations.
- `SemanticKernelController` - `/api/semantickernel/query`
  - AI query endpoint.
- `DocsController` - `/api/docs/preview`, `/api/docs/apply`
  - documentation preview/apply workflow.
- `SecurityAdvisorController` - `/api/securityadvisor/*`
  - SARIF/SBOM analysis, recommendations, PR comments/status, governance overrides/risk/noise policy endpoints.
- `RepoChatController` - `/api/chat/repo`
  - repository-aware chat endpoint.
- `TokenController` - `/api/token/*`
  - personal access token operations.
- `HealthController` - `/api/health/azuredevops`
  - Azure DevOps connectivity check.

## Frontend (adoapi-ui) Index

### Runtime and Tooling

- React 19 + TypeScript + Vite.
- UI stack includes MUI, Axios, Zod validation, MSAL auth.
- Router is defined in `adoapi-ui/src/App.tsx`.

### UI Routes (Current)

- `/docs`
- `/settings`
- `/repos/:repoKey/overview`
- `/repos/:repoKey/memory`
- `/repos/:repoKey/insights`
- `/repos/:repoKey/work-items`
- `/repos/:repoKey/agent-runs`
- `/repos/:repoKey/automation-policy`

### API Client Behavior

`adoapi-ui/src/api/client.ts`:

- Adds `X-Correlation-ID` on all requests.
- Attaches Entra access tokens for `/api/*` calls when MSAL is initialized.
- Handles `401` with one retry after token reacquisition.
- Returns normalized/friendly API errors.
- Validates many responses with Zod schemas.

## Tests

- Project: `ADOApi.Tests/ADOApi.Tests.csproj`.
- Framework: `xUnit`, mocking via `Moq`.
- Includes unit/integration-style tests for services/controllers.

## Configuration Quick Reference

- Backend:
  - `appsettings*.json`
  - user-secrets for local secrets
  - optional Key Vault in production
- Frontend:
  - `VITE_API_BASE_URL`
  - `VITE_ENTRA_CLIENT_ID`
  - `VITE_ENTRA_TENANT_ID`
  - `VITE_API_CLIENT_ID`

## Run and Build Commands

- Restore/build backend:
  - `dotnet restore`
  - `dotnet build ADOApi.sln`
- Run backend:
  - `dotnet run --project ADOApi`
- Run tests:
  - `dotnet test ADOApi.Tests/ADOApi.Tests.csproj`
- Run frontend:
  - `cd adoapi-ui`
  - `npm install`
  - `npm run dev`

## Known Integration Notes

- Frontend calls several `/api/repo/*` and `/api/agent-runs/*` endpoints in `src/api/client.ts`.
- Current controller set in `ADOApi/Controllers` does not include a dedicated `RepoController` for that exact route pattern.
- This suggests either:
  - backend endpoints are still in progress, or
  - frontend API client was built ahead of backend route implementation, or
  - route mapping has shifted and client needs alignment.

## Suggested Next Knowledge Base Expansions

- Add a generated endpoint catalog (method + path + auth policy + request/response DTO).
- Add per-service dependency graph (service -> interfaces -> external clients).
- Add "request traces" for major workflows (Docs preview/apply, Security Advisor PR comment flow).
- Add "config matrix" by environment (dev/test/prod required keys).
