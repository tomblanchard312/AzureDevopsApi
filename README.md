# Azure DevOps API

A .NET 8.0 API for interacting with Azure DevOps, featuring AI-powered work item analysis through Semantic Kernel integration.

## Features

- **Work Item Management**
  - Create, read, update, and delete work items
  - Query work items using WIQL
  - Manage work item relations and templates
  - Filter and search work items

- **Repository Operations**
  - Browse repository contents
  - View file contents
  - Manage branches
  - Track changes

- **AI Integration**
  - Semantic Kernel integration for intelligent work item analysis
  - Azure OpenAI integration for natural language processing
  - AI-powered work item recommendations

- **Authentication & Security**
  - Azure DevOps PAT authentication
  - Secure API endpoints
  - Rate limiting and error handling

## Prerequisites

- .NET 8.0 SDK
- Azure DevOps account with appropriate permissions
- Azure OpenAI service (for AI features)

## Configuration

Create an `appsettings.json` file with the following structure:

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/your-organization",
    "PersonalAccessToken": "your-pat"
  },
  "OpenAI": {
    "DeploymentName": "your-deployment-name",
    "Endpoint": "https://your-openai-endpoint.openai.azure.com/",
    "ApiKey": "your-api-key"
  }
}
```

## Getting Started

1. Clone the repository
2. Configure your `appsettings.json`
3. Run the application:
   ```bash
   dotnet run --project ADOApi
   ```

## API Endpoints

### Work Items
- `GET /api/workitem/workitemtypes` - Get available work item types
- `GET /api/workitem/workitemforproject` - Get all work items for a project
- `POST /api/workitem` - Create a new work item
- `PUT /api/workitem/{id}` - Update a work item
- `GET /api/workitem/{id}` - Get a specific work item
- `POST /api/workitem/filter` - Filter work items

### Repository
- `GET /api/repository/contents` - Get repository contents
- `GET /api/repository/file` - Get file content
- `GET /api/repository/branches` - List branches
- `POST /api/repository/branches` - Create a new branch

### AI Features
- `POST /api/semantic-kernel/ask` - Query work items using natural language
- `POST /api/semantic-kernel/query` - Advanced work item analysis

## Development

### Project Structure
- `ADOApi/` - Main API project
- `ADOApi.UI/` - Blazor WebAssembly frontend
- `ADOApi.Tests/` - Unit and integration tests

### Dependencies
- Microsoft.TeamFoundationServer.ExtendedClient
- Microsoft.SemanticKernel
- Microsoft.SemanticKernel.Connectors.OpenAI
- Azure.AI.OpenAI

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

