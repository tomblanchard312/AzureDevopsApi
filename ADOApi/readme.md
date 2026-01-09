# Azure DevOps API

A .NET 8 API for interacting with Azure DevOps services using Entra ID authentication and AI integration.

## Configuration

The application requires the following configuration settings:

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/your-organization",
    "UseEntraAuth": true
  },
  "AzureDevOpsEntra": {
    "TenantId": "[Your Tenant ID]",
    "ClientId": "[Your Client ID]",
    "ClientSecret": "[Stored in Key Vault]",
    "Scopes": ["499b84ac-1321-427f-aa17-267ca6975798/.default"]
  },
  "LLM": {
    "Provider": "AzureOpenAI"
  }
}
```

### Required Settings

- OrganizationUrl: Your Azure DevOps organization URL
- UseEntraAuth: Set to true for Entra ID authentication
- TenantId: Your Azure AD tenant ID
- ClientId: Service principal client ID
- ClientSecret: Service principal client secret (stored securely)
- LLM.Provider: AI provider ("AzureOpenAI" or "Ollama")

## Features

- Work Item Management with AI analysis
- Repository Operations
- Project Information
- Template Management
- Query Execution
- Health Checks

## API Endpoints

The API provides various endpoints for interacting with Azure DevOps services. See the Swagger documentation for detailed endpoint information.

## Authentication

The API uses Azure DevOps Personal Access Tokens (PAT) for authentication. Ensure your PAT has the necessary permissions for the operations you need to perform.

## Overview

The AzureDevOpsService API is a .NET-based interface designed to interact with Azure DevOps services. It provides functionalities to manage and query work items, iterations, and projects within an Azure DevOps organization.

## Note

This API is a work in progress, and the functionality is subject to change. Users are encouraged to regularly check for updates and modifications as the API evolves.

## Contributing

Contributions to the AzureDevOpsService API are welcome. If you have suggestions or improvements, please submit a pull request or open an issue in the repository.

## Contact

For any questions or feedback regarding this API, please reach out to me or submit an issue in the project's issue tracker.
