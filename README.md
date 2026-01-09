# AzureDevopsApi

A .NET 8 ASP.NET Core API that provides RESTful access to Azure DevOps operations, including work item management, repository interactions, and AI-powered analysis using Microsoft Semantic Kernel and Azure OpenAI.

## Overview

This project exposes Azure DevOps functionality via HTTP endpoints, enabling integration with external tools or UIs. It includes AI features for natural language queries on work items, with built-in authentication, caching, and error handling. The API supports versioning and is designed for deployment in Azure environments.

## Key Features

- **Work Item Management**: CRUD operations on work items, WIQL queries, relations, and templates.
- **Repository Operations**: Browse contents, view files, manage branches, and track changes.
- **AI Integration**: Semantic Kernel for intelligent work item analysis and recommendations via Azure OpenAI.
- **Authentication & Security**: JWT Bearer authentication with role-based policies, rate limiting, and input sanitization.
- **Caching & Performance**: In-memory caching for read-heavy endpoints with ETag support.
- **Resilience**: Polly retry policies for Azure DevOps calls and optimistic concurrency for updates.

## Technology Stack

- **Framework**: .NET 8, ASP.NET Core MVC
- **Azure Integrations**: Azure DevOps SDK (Microsoft.TeamFoundationServer.Client), Azure OpenAI (via Semantic Kernel), Azure Key Vault, Azure Identity
- **AI**: Microsoft Semantic Kernel 1.0.0-rc4, Azure OpenAI Chat Completion
- **Authentication**: Microsoft Identity Web 3.0.0 (JWT Bearer)
- **Other**: Polly 8.2.1 (resilience), Serilog 3.1.1 (logging), NSwag 14.0.3 (API docs), xUnit (testing)

## Prerequisites

- .NET 8.0 SDK
- Azure DevOps organization with a Personal Access Token (PAT) for API access
- Azure OpenAI resource with deployment configured
- Azure subscription (for Key Vault in production)

## Local Development Setup

1. Clone the repository:

   ```bash
   git clone https://github.com/tomblanchard312/AzureDevopsApi.git
   cd AzureDevopsApi
   ```

2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Configure secrets (see Configuration section).

4. Run the API:

   ```bash
   dotnet run --project ADOApi
   ```

   The API will start at `https://localhost:5001` (or configured port) with Swagger UI at `/swagger`.

5. (Optional) Run the Blazor UI:

   ```bash
   dotnet run --project ADOApi.UI
   ```

6. Run tests:
   ```bash
   dotnet test ADOApi.Tests/ADOApi.Tests.csproj
   ```

## Configuration

Configuration is managed via `appsettings.json`, environment variables, user-secrets (development), and Azure Key Vault (production). Secrets must not be committed.

### Non-Secret Configuration (appsettings.json)

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/your-org"
  },
  "OpenAI": {
    "DeploymentName": "your-deployment",
    "Endpoint": "https://your-endpoint.openai.azure.com/"
  },
  "KeyVault": {
    "VaultUri": "https://your-vault.vault.azure.net/"
  }
}
```

### Secrets (User-Secrets for Development)

```bash
cd ADOApi
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "AzureDevOps:PersonalAccessToken" "your-pat"
```

### Production (Azure Key Vault)

- Store secrets in Key Vault (e.g., `OpenAI--ApiKey` for `OpenAI:ApiKey`).
- Assign managed identity to the app with Key Vault access.
- The app uses `DefaultAzureCredential` for authentication.

Startup validates required secrets (`OpenAI:ApiKey`, `AzureDevOps:PersonalAccessToken`) and fails if missing.

## API Overview

The API uses URL-segment versioning (e.g., `/api/v1.0/project/...`). Endpoints require authentication via JWT Bearer tokens with roles like `ADO.ReadOnly` or `ADO.Contributor`.

- **Work Items**: `/api/workitem` - CRUD, queries, updates with optimistic concurrency.
- **Repositories**: `/api/repository` - File operations, branches, with base commit IDs for updates.
- **Projects**: `/api/project` - Iterations, projects list.
- **AI Queries**: `/api/semantickernel/query` - Batched work item analysis with sanitized inputs.

Swagger documentation is available at `/swagger` for interactive testing. The optional Blazor UI provides a web interface for basic operations.

## Troubleshooting

- **Build Errors**: Ensure .NET 8 SDK is installed and packages are restored.
- **Startup Failures**: Check for missing secrets in configuration.
- **Test Failures**: Verify external dependencies (Azure DevOps, OpenAI) are accessible.
- **Performance Issues**: Review caching and async method implementations.
