# Developer Guide

This guide helps contributors understand the codebase and contribute safely.

## Project Structure

- **ADOApi/**: Main API project.
  - **Controllers/**: HTTP endpoints (e.g., `RepositoryController`).
  - **Services/**: Business logic (e.g., `WorkItemService`).
  - **Interfaces/**: Contracts for DI (e.g., `IWorkItemService`).
  - **Models/**: DTOs (e.g., `WorkItemFilterRequest`).
  - **Utilities/**: Middleware and helpers (e.g., `RateLimitingMiddleware`).
  - **Exceptions/**: Custom errors (e.g., `AzureDevOpsApiException`).
  - **Program.cs**: DI setup and startup.
- **ADOApi.UI/**: Optional Blazor WASM frontend.
- **ADOApi.Tests/**: xUnit tests with `WebApplicationFactory` for integration.

## Coding Conventions

- **Naming**: PascalCase for classes/methods/properties; async methods end with `Async`.
- **Dependency Injection**: Register services in `Program.cs`; use interfaces for testability.
- **Error Handling**: Throw `AzureDevOpsApiException` with `StatusCode`; map to HTTP in controllers.
- **Async/Await**: Use async for I/O; avoid blocking calls.
- **Logging**: Use `ILogger<T>` with structured messages; sanitize user inputs.
- **Nullability**: Enable nullable references; handle nulls explicitly.

## Adding Features Safely

1. **Plan**: Identify controller, service, and interface changes.
2. **Implement**: Add interface first, then service and controller. Use DI.
3. **Security**: Add auth policies if needed; sanitize inputs.
4. **Caching/Concurrency**: Use `ICachingService` for reads; require IDs for updates.
5. **Test**: Write unit/integration tests before committing.

## Testing Strategy

- **Unit Tests**: Test services in isolation with mocks.
- **Integration Tests**: Use `WebApplicationFactory` in `ADOApi.Tests` for full stack.
- **Run Tests**: `dotnet test ADOApi.Tests/ADOApi.Tests.csproj`.
- **Coverage**: Aim for key paths; check for regressions.

## Common Pitfalls

- **Async Issues**: Methods like `AzureDevOpsService` lack await—fix to prevent blocking.
- **Nullability Warnings**: CS8604 errors from dereferencing nulls—add checks.
- **Logging Leaks**: SARIF warns on user data in logs—use placeholders.
- **Hardcoded Config**: Values like batch sizes—move to `IConfiguration`.
- **Auth Gaps**: Endpoints without explicit policies—add `[Authorize]`.

## Guidance for Extending Semantic Kernel Features

- **Adding Plugins**: Extend `SemanticChatService` to register new plugins in the Kernel builder.
- **Custom Prompts**: Modify system messages in `SemanticKernelController` for new behaviors.
- **Safety**: Always sanitize inputs and enforce JSON responses to prevent injection.
- **Testing**: Mock `ISemanticChatService` for unit tests; use integration tests for end-to-end AI flows.
