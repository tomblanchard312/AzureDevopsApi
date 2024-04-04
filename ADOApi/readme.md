# AzureDevOpsService API

## Overview
The AzureDevOpsService API is a .NET-based interface designed to interact with Azure DevOps services. It provides functionalities to manage and query work items, iterations, and projects within an Azure DevOps organization. This API is currently under development and features may be added, removed, or changed in future releases.

## Configuration
Replace the following in the Appsettings with your values:
"AzureDevOps": {
    "Organization": "[Your Org]",
    "PersonalAccessToken": "[Your Pat]",
    "AdminPat": "[Admin Pat]",
    "Project": "[Your Project]"
  },

## Authentication
Admin Token: To create personal access tokens through this API, an admin token with elevated permissions is required. This ensures that only authorized users can generate new tokens.
Personal Access Token: For general operations like reading and writing work items, querying projects, and managing iterations, a personal access token with appropriate permissions to read and write work items is sufficient.
Current Capabilities
The API currently supports a variety of operations, including but not limited to:

Retrieving work item types for a specific project.
Fetching iterations within a project.
Listing all projects within the organization.
Getting work items by type.
Creating personal access tokens (requires an admin token).
Adding new work items to a project.
Retrieving and updating existing work items.

## Note
This API is a work in progress, and the functionality is subject to change. Users are encouraged to regularly check for updates and modifications as the API evolves.
It currently needs some cleanup and refactoring. 
It may no longer need the project to be in app settings, as it is not retrieved from your org.
Other various things that will be updated.

## Contributing

Contributions to the AzureDevOpsService API are welcome. If you have suggestions or improvements, please submit a pull request or open an issue in the repository.

## Contact
For any questions or feedback regarding this API, please reach out to me or submit an issue in the project's issue tracker.

