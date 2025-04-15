# AzureDevOpsService API

## Overview
The AzureDevOpsService API is a .NET 8.0-based REST API designed to interact with Azure DevOps services. It provides a comprehensive interface for managing work items, iterations, and projects within an Azure DevOps organization. The API is built with modern .NET practices and includes features like API versioning, Swagger documentation, and dependency injection.

## Technical Stack
- **Framework**: .NET 8.0
- **Architecture**: REST API with MVC pattern
- **Key Dependencies**:
  - Microsoft.AspNetCore.Mvc.Versioning (5.1.0)
  - Microsoft.TeamFoundationServer.Client (19.225.1)
  - Swashbuckle.AspNetCore (6.5.0)
  - Serilog (3.1.1)
  - Microsoft.IdentityModel.JsonWebTokens (7.5.0)
  - Polly (8.2.1) - For retry policies and resilience
  - Microsoft.AspNetCore.Mvc.NewtonsoftJson (8.0.0) - For JSON handling

## Features
- **Work Item Management**
  - Create, read, update, and delete work items
  - Query work items by type
  - Manage work item relationships
  - Work item templates support
    - Create and manage work item templates
    - Create work items from templates
    - List and delete templates
- **Project Management**
  - List all projects in the organization
  - Retrieve project details
  - Manage project configurations
- **Iteration Management**
  - List iterations within a project
  - Create and manage iterations
- **Authentication & Authorization**
  - Personal Access Token (PAT) management
  - Admin token support for elevated operations
  - Secure token handling and validation
- **Resilience & Error Handling**
  - Retry policies for transient failures
  - Comprehensive error handling
  - Detailed logging with Serilog

## Configuration
1. Update the `appsettings.json` with your Azure DevOps configuration:
```json
{
  "AzureDevOps": {
    "Organization": "[Your Org]",
    "PersonalAccessToken": "[Your PAT]",
    "AdminPat": "[Admin Pat]",
    "Project": "[Your Project]"
  }
}
```

2. Required Permissions:
   - Admin Token: Required for PAT management and elevated operations
   - Personal Access Token: Needs permissions for work item read/write operations

## API Documentation
The API includes Swagger documentation, accessible at:
- Swagger UI: `{baseUrl}/swagger`
- Swagger JSON: `{baseUrl}/swagger/v1/swagger.json`

## API Versioning
The API supports versioning through URL segments:
- Default version: v1.0
- Version format: `{version}/api/{controller}/{action}/{id?}`
- Example: `/v1.0/api/workitems/get/123`

## Development Setup
1. Clone the repository
2. Restore NuGet packages
3. Update configuration in `appsettings.json`
4. Run the application:
   ```bash
   dotnet run --project ADOApi
   ```

## Project Structure
```
ADOApi/
├── Controllers/     # API endpoints
├── Models/         # Data models
├── Services/       # Business logic
├── Interfaces/     # Service contracts
├── Utilities/      # Helper functions
├── Exceptions/     # Custom exception handling
└── Properties/     # Application properties
```

## Security Considerations
- All tokens and sensitive information should be stored securely
- Use environment variables or secure configuration management in production
- Implement proper access controls and authentication mechanisms

## Contributing
Contributions to the AzureDevOpsService API are welcome. Please follow these steps:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request with a clear description of changes

## Development Status
- The project is under active development
- Recent improvements:
  - Added work item template support
  - Implemented retry policies with Polly
  - Enhanced error handling and logging
  - Updated package dependencies
- Planned improvements:
  - Refactoring of project configuration
  - Additional API endpoints
  - Performance optimizations

## Support
For questions or issues:
- Open an issue in the repository
- Contact the maintainers through the project's issue tracker

## License
This project is licensed under the terms specified in the LICENSE.txt file.

## UI Development Setup
The project includes support for generating a TypeScript client and UI using NSwag. To set up the UI:

1. Install the required tools:
   ```bash
   dotnet tool install -g NSwag.ConsoleCore
   ```

2. Create a new Angular project (if not already created):
   ```bash
   ng new ClientApp
   cd ClientApp
   npm install
   ```

3. Generate the TypeScript client:
   ```bash
   nswag run nswag.json
   ```

4. Configure the API base URL in your Angular environment:
   ```typescript
   // src/environments/environment.ts
   export const environment = {
     production: false,
     apiBaseUrl: 'http://localhost:5000'
   };
   ```

5. Create a service to use the generated client:
   ```typescript
   // src/app/services/api.service.ts
   import { Injectable } from '@angular/core';
   import { environment } from '../../environments/environment';
   import { WorkItemClient } from './api-client';

   @Injectable({
     providedIn: 'root'
   })
   export class ApiService {
     private workItemClient: WorkItemClient;

     constructor() {
       this.workItemClient = new WorkItemClient(environment.apiBaseUrl);
     }

     // Add methods to interact with the API
     async getWorkItems(project: string) {
       return await this.workItemClient.getAllWorkItemsForProject(project);
     }
   }
   ```

6. Create components to display the data:
   ```typescript
   // src/app/components/work-item-list/work-item-list.component.ts
   import { Component, OnInit } from '@angular/core';
   import { ApiService } from '../../services/api.service';

   @Component({
     selector: 'app-work-item-list',
     template: `
       <div *ngFor="let item of workItems">
         <h3>{{item.title}}</h3>
         <p>{{item.description}}</p>
       </div>
     `
   })
   export class WorkItemListComponent implements OnInit {
     workItems: any[] = [];

     constructor(private apiService: ApiService) {}

     async ngOnInit() {
       this.workItems = await this.apiService.getWorkItems('your-project');
     }
   }
   ```

7. Add the component to your app module:
   ```typescript
   // src/app/app.module.ts
   import { WorkItemListComponent } from './components/work-item-list/work-item-list.component';

   @NgModule({
     declarations: [
       AppComponent,
       WorkItemListComponent
     ],
     // ...
   })
   ```

8. Run the Angular development server:
   ```bash
   ng serve
   ```

The UI will be available at `http://localhost:4200`.

