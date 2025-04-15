using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Linq;
using ADOApi.Models;
using ADOApi.Utilities;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using ADOApi.Interfaces;
using System.Security.Permissions;
using Microsoft.VisualStudio.Services.Organization.Client;
using Microsoft.VisualStudio.Services.TestManagement.TestPlanning.WebApi;
using System.Net.Http;
using WorkItemDetails = ADOApi.Models.WorkItemDetails;
using WorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ADOApi.Exceptions;
using Microsoft.TeamFoundation.Core.WebApi;

namespace ADOApi.Services
{
    public class AzureDevOpsService : IAzureDevOpsService
    {
        private readonly IWorkItemService _workItemService;
        private readonly ILogger<AzureDevOpsService> _logger;
        private readonly ProjectHttpClient _projectClient;

        public AzureDevOpsService(
            IWorkItemService workItemService,
            ILogger<AzureDevOpsService> logger,
            VssConnection connection)
        {
            _workItemService = workItemService;
            _logger = logger;
            _projectClient = connection.GetClient<ProjectHttpClient>();
        }

        public async Task<List<string>> GetProjectsAsync()
        {
            try
            {
                var projects = await _projectClient.GetProjects();
                return projects.Select(p => p.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve projects");
                throw new AzureDevOpsApiException("Failed to retrieve projects", ex);
            }
        }

        public async Task<List<string>> GetWorkItemTypesAsync(string project)
        {
            try
            {
                var workItemTypes = await _workItemService.GetWorkItemTypesAsync(project);
                return workItemTypes.Select(wit => wit.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve work item types");
                throw new AzureDevOpsApiException("Failed to retrieve work item types", ex);
            }
        }

        public async Task<List<string>> GetIterationsAsync(string project)
        {
            try
            {
                var iterations = await _workItemService.GetIterationsAsync(project);
                return iterations.Select(i => i.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve iterations");
                throw new AzureDevOpsApiException("Failed to retrieve iterations", ex);
            }
        }

        public async Task<List<WorkItem>> GetWorkItemsByTypeAsync(string project, string workItemType)
        {
            // Implementation for retrieving work items by type
            throw new NotImplementedException();
        }

        public async Task<string> CreatePersonalAccessTokenAsync(string displayName, string scope, DateTime validTo, bool allOrgs = false)
        {
            // Implementation for creating a personal access token
            throw new NotImplementedException();
        }

        public async Task<List<PatResponse>> GetTokensAsync()
        {
            // Implementation for retrieving personal access tokens
            throw new NotImplementedException();
        }

        public async Task<int> AddWorkItemAsync(string project, string workItemType, string title, string description, string assignedTo, string tag, double? effortHours = null, string? comments = null, int? parentId = null)
        {
            // Implementation for adding a new work item
            throw new NotImplementedException();
        }

        public async Task<List<RecentWorkItem>> GetRecentWorkItemsAsync()
        {
            // Implementation for retrieving recent work items
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateWorkItemAsync(int workItemId, string state, string comment, string assignedUser, int? priority, double? remainingEffortHours, double? completedEffortHours, string? tag)
        {
            // Implementation for updating a work item
            throw new NotImplementedException();
        }

        public async Task<int> CreateWorkItemTemplateAsync(Models.WorkItemTemplate template)
        {
            try
            {
                var patchDocument = new JsonPatchDocument();
                
                // Add required fields
                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Title",
                        Value = template.Title ?? "Template: " + template.Name
                    }
                );

                // Add optional fields if provided
                if (!string.IsNullOrEmpty(template.Description))
                {
                    patchDocument.Add(
                        new JsonPatchOperation()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.Description",
                            Value = template.Description
                        }
                    );
                }

                if (!string.IsNullOrEmpty(template.AssignedTo))
                {
                    patchDocument.Add(
                        new JsonPatchOperation()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.AssignedTo",
                            Value = template.AssignedTo
                        }
                    );
                }

                if (!string.IsNullOrEmpty(template.State))
                {
                    patchDocument.Add(
                        new JsonPatchOperation()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.State",
                            Value = template.State
                        }
                    );
                }

                if (template.Priority.HasValue)
                {
                    patchDocument.Add(
                        new JsonPatchOperation()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/Microsoft.VSTS.Common.Priority",
                            Value = template.Priority.Value
                        }
                    );
                }

                if (template.EffortHours.HasValue)
                {
                    patchDocument.Add(
                        new JsonPatchOperation()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/Microsoft.VSTS.Scheduling.Effort",
                            Value = template.EffortHours.Value
                        }
                    );
                }

                if (!string.IsNullOrEmpty(template.Tags))
                {
                    patchDocument.Add(
                        new JsonPatchOperation()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.Tags",
                            Value = template.Tags
                        }
                    );
                }

                // Add custom fields if provided
                if (template.CustomFields != null)
                {
                    foreach (var field in template.CustomFields)
                    {
                        patchDocument.Add(
                            new JsonPatchOperation()
                            {
                                Operation = Operation.Add,
                                Path = $"/fields/{field.Key}",
                                Value = field.Value
                            }
                        );
                    }
                }

                // Create the work item
                var workItem = await _workItemService.CreateWorkItemAsync(
                    patchDocument,
                    template.Project,
                    template.WorkItemType);

                return workItem.Id.Value;
            }
            catch (Exception ex)
            {
                throw new AzureDevOpsApiException("Failed to create work item template", ex);
            }
        }

        public async Task<List<Models.WorkItemTemplate>> GetWorkItemTemplatesAsync(string project)
        {
            try
            {
                var query = new Wiql()
                {
                    Query = $"SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Template' AND [System.TeamProject] = '{project}'"
                };

                var queryResult = await _workItemService.QueryByWiqlAsync(query, project);
                if (queryResult.WorkItems.Any())
                {
                    var workItems = await _workItemService.GetWorkItemsAsync(
                        queryResult.WorkItems.Select(wi => wi.Id).ToList(),
                        expand: WorkItemExpand.All);

                    return workItems.Select(wi => new Models.WorkItemTemplate
                    {
                        Id = wi.Id.Value,
                        Name = wi.Fields["System.Title"]?.ToString() ?? string.Empty,
                        WorkItemType = wi.Fields["System.WorkItemType"]?.ToString() ?? string.Empty,
                        Project = project,
                        Title = wi.Fields["System.Title"]?.ToString(),
                        Description = wi.Fields["System.Description"]?.ToString(),
                        AssignedTo = wi.Fields["System.AssignedTo"]?.ToString(),
                        State = wi.Fields["System.State"]?.ToString(),
                        Priority = wi.Fields["Microsoft.VSTS.Common.Priority"] as int?,
                        EffortHours = wi.Fields["Microsoft.VSTS.Scheduling.Effort"] as double?,
                        Tags = wi.Fields["System.Tags"]?.ToString(),
                        CustomFields = wi.Fields
                            .Where(f => !f.Key.StartsWith("System.") && !f.Key.StartsWith("Microsoft."))
                            .ToDictionary(f => f.Key, f => f.Value)
                    }).ToList();
                }

                return new List<Models.WorkItemTemplate>();
            }
            catch (Exception ex)
            {
                throw new AzureDevOpsApiException("Failed to retrieve work item templates", ex);
            }
        }

        public async Task<int> CreateWorkItemFromTemplateAsync(int templateId, Dictionary<string, object>? overrides = null)
        {
            try
            {
                var template = await _workItemService.GetWorkItemAsync(templateId, expand: WorkItemExpand.All);
                var patchDocument = new JsonPatchDocument();

                // Copy all fields from template
                foreach (var field in template.Fields)
                {
                    if (!field.Key.StartsWith("System.") || field.Key == "System.Title")
                    {
                        patchDocument.Add(
                            new JsonPatchOperation()
                            {
                                Operation = Operation.Add,
                                Path = $"/fields/{field.Key}",
                                Value = field.Value
                            }
                        );
                    }
                }

                // Apply overrides if provided
                if (overrides != null)
                {
                    foreach (var overrideField in overrides)
                    {
                        patchDocument.Add(
                            new JsonPatchOperation()
                            {
                                Operation = Operation.Add,
                                Path = $"/fields/{overrideField.Key}",
                                Value = overrideField.Value
                            }
                        );
                    }
                }

                // Create the new work item
                var workItem = await _workItemService.CreateWorkItemAsync(
                    patchDocument,
                    template.Fields["System.TeamProject"].ToString(),
                    template.Fields["System.WorkItemType"].ToString());

                return workItem.Id.Value;
            }
            catch (Exception ex)
            {
                throw new AzureDevOpsApiException("Failed to create work item from template", ex);
            }
        }

        public async Task DeleteWorkItemTemplateAsync(int templateId)
        {
            try
            {
                await _workItemService.DeleteWorkItemAsync(templateId);
            }
            catch (Exception ex)
            {
                throw new AzureDevOpsApiException("Failed to delete work item template", ex);
            }
        }

        public async Task<List<WorkItem>> GetAllWorkItemsForProjectAsync(string project)
        {
            try
            {
                var query = new Wiql()
                {
                    Query = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{project}'"
                };

                var queryResult = await _workItemService.QueryByWiqlAsync(query, project);
                if (queryResult.WorkItems.Any())
                {
                    return await _workItemService.GetWorkItemsAsync(
                        queryResult.WorkItems.Select(wi => wi.Id).ToList(),
                        expand: WorkItemExpand.All);
                }

                return new List<WorkItem>();
            }
            catch (Exception ex)
            {
                throw new AzureDevOpsApiException("Failed to retrieve work items for project", ex);
            }
        }

        public async Task<List<WorkItem>> GetMyAssignedWorkItemsAsync(string project, string userIdentifier)
        {
            try
            {
                var query = new Wiql()
                {
                    Query = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{project}' AND [System.AssignedTo] = '{userIdentifier}'"
                };

                var queryResult = await _workItemService.QueryByWiqlAsync(query, project);
                if (queryResult.WorkItems.Any())
                {
                    return await _workItemService.GetWorkItemsAsync(
                        queryResult.WorkItems.Select(wi => wi.Id).ToList(),
                        expand: WorkItemExpand.All);
                }

                return new List<WorkItem>();
            }
            catch (Exception ex)
            {
                throw new AzureDevOpsApiException("Failed to retrieve assigned work items", ex);
            }
        }

        public async Task<WorkItem> GetWorkItemByIdAsync(int workItemId, string project)
        {
            try
            {
                return await _workItemService.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All);
            }
            catch (Exception ex)
            {
                throw new AzureDevOpsApiException("Failed to retrieve work item", ex);
            }
        }
    }
}
