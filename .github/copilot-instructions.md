# Copilot instructions for AzureDevopsApi

This repository is a .NET 8 API that surfaces Azure DevOps operations and AI features (Semantic Kernel / Azure OpenAI). Keep instructions short, actionable, and code-aware so an AI coding agent can be productive immediately.

Goals for code suggestions

- Prefer small, focused changes that respect existing DI registrations in `ADOApi/Program.cs` and `Startup`.
- Preserve API routing and versioning: controllers use URL-segment versioning (pattern: `{version:apiVersion=1.0}/api/{controller}/{action}/{id?}`). See `ADOApi/Program.cs` for configuration.
- Follow existing error handling: services throw `AzureDevOpsApiException` and use structured logging; surface errors to `GlobalExceptionHandler` middleware.

Key architecture notes (big picture)

- API project: [ADOApi](ADOApi) — controllers under `ADOApi/Controllers/`, services under `ADOApi/Services/`, interfaces in `ADOApi/Interfaces/`.
- Frontend: [ADOApi.UI](ADOApi.UI) — Blazor WASM app (optional, separate host).
- DI and clients: `VssConnection` is registered in `Program.cs` and used to obtain `WorkItemTrackingHttpClient`, `GitHttpClient`, and `ProjectHttpClient`. Implement changes by updating the concrete services and keeping constructor signatures stable.
- AI: Semantic Kernel is configured as a singleton in `Program.cs` using `AddAzureOpenAIChatCompletion`. Required config keys: `OpenAI:DeploymentName`, `OpenAI:Endpoint`, `OpenAI:ApiKey`.
- Retries and resilience: service classes use Polly retry policies (ex: `RepositoryService`, `AzureDevOpsService`). Reuse the same style and logging on retries.

Important config and runtime commands

- Required config keys (in `appsettings.json` or environment):
  - `AzureDevOps:OrganizationUrl`
  - `AzureDevOps:PersonalAccessToken`
  - `OpenAI:DeploymentName`, `OpenAI:Endpoint`, `OpenAI:ApiKey`
- Build and run (development):
  - `dotnet build ADOApi.sln`
  - `dotnet run --project ADOApi` (API + Swagger hosted at `/`)
  - `dotnet run --project ADOApi.UI` (run frontend separately)

Controller & route examples (use when adding endpoints)

- ProjectController: `GET /api/project/iterations?project={name}` and `GET /api/project/projects` — see `ADOApi/Controllers/ProjectController.cs`.
- Repository endpoints are implemented in `RepositoryService` and exposed by `RepositoryController` (look at service methods for parameter shapes). Use the same route/action naming conventions when adding new controllers.

Common code patterns to follow

- Use DI interfaces defined in `ADOApi/Interfaces/` and register implementations in `Program.cs` (e.g., `services.AddScoped<IRepositoryService, RepositoryService>();`).
- Use `JsonPatchDocument` (Microsoft.VisualStudio.Services.WebApi.Patch.Json) for work item creation/updates — see `AzureDevOpsService.CreateWorkItemTemplateAsync` for an example.
- Throw `AzureDevOpsApiException` for service-layer failures and include the inner exception for traceability.
- Use structured logging (`ILogger<T>`) and follow existing log messages and retry logging patterns.

Integration points and external dependencies

- Azure DevOps REST/SDK via `VssConnection` and typed clients (`WorkItemTrackingHttpClient`, `GitHttpClient`, `ProjectHttpClient`).
- Microsoft.SemanticKernel and Azure OpenAI for AI features (configured in Program.cs).
- Polly for resilience; follow existing exponential-backoff policy usage.

Files to inspect when making changes

- `ADOApi/Program.cs` — DI, client setup, Semantic Kernel config, middleware, swagger, routing.
- `ADOApi/Services/*` — concrete implementations (retry policies, Azure DevOps calls).
- `ADOApi/Interfaces/*` — public contracts for services.
- `ADOApi/Controllers/*` — API surface and route conventions.
- `ADOApi/Utilities/GlobalExceptionHandler.cs` and `ADOApi/Utilities/RateLimitingMiddleware.cs` — global behavior.

Developer guidance for PRs

- Keep changes minimal and unit-targeted: add or update one service/controller pair per PR.
- Ensure DI registrations remain unchanged unless intentionally adding a new service binding.
- Add logging and wrap external calls in the same retry-policy style.

If unclear or missing

- If a setting or required behavior isn't discoverable in code, ask the maintainer for the intended behavior and a sample `appsettings.json` value.

After applying changes, ask the developer whether to run builds or add unit tests.

---

If you'd like, I can now create a short checklist to validate PRs against these conventions or run `dotnet build` and report results.
