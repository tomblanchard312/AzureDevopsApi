# Azure DevOps API

A .NET Core API for interacting with Azure DevOps services.

## Configuration

The application requires the following configuration settings:

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/your-organization",
    "PersonalAccessToken": "[Your PAT]"
  }
}
```

### Required Settings

- OrganizationUrl: Your Azure DevOps organization URL
- PersonalAccessToken: Your Azure DevOps Personal Access Token with appropriate permissions

## Features

- Work Item Management
- Project Information
- Template Management
- Query Execution

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

