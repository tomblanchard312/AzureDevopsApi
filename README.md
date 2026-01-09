# AzureDevopsApi

A .NET 8 ASP.NET Core API that provides RESTful access to Azure DevOps operations, including work item management, repository interactions, and AI-powered analysis using Microsoft Semantic Kernel with support for Azure OpenAI and local Ollama instances.

## Overview

This project exposes Azure DevOps functionality via HTTP endpoints, enabling integration with external tools or UIs. It includes AI features for natural language queries on work items, with built-in authentication, caching, and error handling. The API supports versioning and is designed for deployment in Azure environments.

## Key Features

- **Work Item Management**: CRUD operations on work items, WIQL queries, relations, and templates.
- **Repository Operations**: Browse contents, view files, manage branches, and track changes.
- **AI Integration**: Semantic Kernel for intelligent work item analysis with runtime switching between Azure OpenAI and Ollama.
- **Authentication & Security**: JWT Bearer authentication with role-based policies, rate limiting, and input sanitization. Azure DevOps access via Entra ID service principal.
- **Caching & Performance**: In-memory caching for read-heavy endpoints with ETag support.
- **Resilience**: Polly retry policies for Azure DevOps calls and optimistic concurrency for updates.
- **Health Checks**: Admin-only endpoints for validating Azure DevOps connectivity.

## Technology Stack

- **Framework**: .NET 8, ASP.NET Core MVC
- **Azure Integrations**: Azure DevOps SDK (Microsoft.TeamFoundationServer.Client), Azure OpenAI (via Semantic Kernel), Azure Key Vault, Azure Identity, Microsoft Identity Web
- **AI**: Microsoft Semantic Kernel 1.0.0-rc4, Azure OpenAI Chat Completion, Ollama REST API
- **Authentication**: Microsoft Identity Web 3.0.0 (JWT Bearer), MSAL for Entra service principal
- **Other**: Polly 8.2.1 (resilience), Serilog 3.1.1 (logging), NSwag 14.0.3 (API docs), xUnit (testing)

## Prerequisites

- .NET 8.0 SDK
- Azure DevOps organization with Entra ID service principal configured
- Azure OpenAI resource with deployment configured (optional, can use Ollama)
- Local Ollama instance (optional, for local AI processing)
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

5. (Optional) Run the React UI:

   ```bash
   cd adoapi-ui
   npm install
   npm run dev
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
    "OrganizationUrl": "https://dev.azure.com/your-org",
    "UseEntraAuth": true
  },
  "AzureDevOpsEntra": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-service-principal-client-id",
    "Scopes": ["499b84ac-1321-427f-aa17-267ca6975798/.default"],
    "AuthorityHost": "https://login.microsoftonline.com/"
  },
  "LLM": {
    "Provider": "AzureOpenAI"
  },
  "Ollama": {
    "Model": "qwen2.5-coder"
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
dotnet user-secrets set "AzureDevOpsEntra:ClientSecret" "your-client-secret"
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key"
```

### Production (Azure Key Vault)

- Store secrets in Key Vault (e.g., `AzureDevOpsEntra--ClientSecret` for `AzureDevOpsEntra:ClientSecret`).
- Assign managed identity to the app with Key Vault access.
- The app uses `DefaultAzureCredential` for authentication.

Startup validates required secrets based on configuration (e.g., `AzureDevOpsEntra:ClientSecret` when using Entra auth, `OpenAI:ApiKey` when using Azure OpenAI).

## API Overview

The API uses URL-segment versioning (e.g., `/api/v1.0/project/...`). Endpoints require authentication via JWT Bearer tokens with roles like `ADO.ReadOnly` or `ADO.Contributor`.

- **Work Items**: `/api/workitem` - CRUD, queries, updates with optimistic concurrency.
- **Repositories**: `/api/repository` - File operations, branches, with base commit IDs for updates.
- **Projects**: `/api/project` - Iterations, projects list.
- **AI Queries**: `/api/semantickernel/query` - Batched work item analysis with sanitized inputs.
- **Health Checks**: `/api/health/azuredevops` - Admin-only connectivity validation (ADO.Admin role required).

Swagger documentation is available at `/swagger` for interactive testing. The optional React UI provides a web interface for basic operations.

## Security Features

- **Secret Scanning**: GitHub Actions workflow using Gitleaks blocks pushes/PRs with detected secrets.
- **Authentication**: JWT Bearer tokens with role-based access control.
- **Azure DevOps Access**: Entra ID service principal with scoped permissions.
- **Input Sanitization**: All AI inputs are sanitized to prevent injection attacks.
- **Rate Limiting**: Configurable limits per endpoint and user role.

## Troubleshooting

- **Build Errors**: Ensure .NET 8 SDK is installed and packages are restored.
- **Startup Failures**: Check for missing secrets in configuration and validate Entra service principal setup.
- **Authentication Issues**: Verify JWT tokens include required roles and Azure DevOps permissions.
- **AI Provider Switching**: Update `LLM:Provider` config and ensure Ollama is running locally if selected.
- **Test Failures**: Verify external dependencies (Azure DevOps, OpenAI/Ollama) are accessible.
