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
using ADOApi.Services;
using Microsoft.Extensions.Configuration;
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
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace ADOApi.Services
{
    public class AzureDevOpsService : IAzureDevOpsService
    {
        // In-memory metadata store for PATs. Production should replace this with secure, append-only storage.
        private readonly List<PatMetadata> _patMetadataStore = new();

        private readonly IWorkItemService _workItemService;
        private readonly ILogger<AzureDevOpsService> _logger;
        private readonly ProjectHttpClient _projectClient;
        private readonly WorkItemTrackingHttpClient _workItemTrackingHttpClient;
        private readonly ResiliencePolicies _policies;
        private readonly ICachingService _cache;
        private readonly IConfiguration _configuration;

        public AzureDevOpsService(
            IWorkItemService workItemService,
            ILogger<AzureDevOpsService> logger,
            VssConnection connection,
            ResiliencePolicies policies,
            ICachingService cache,
            IConfiguration configuration)
        {
            _workItemService = workItemService;
            _logger = logger;
            _projectClient = connection.GetClient<ProjectHttpClient>();
            _workItemTrackingHttpClient = connection.GetClient<WorkItemTrackingHttpClient>();
            _policies = policies;
            _cache = cache;
            _configuration = configuration;
        }

        public async Task<List<string>> GetProjectsAsync()
        {
            try
            {
                var org = _configuration["AzureDevOps:OrganizationUrl"]?.TrimEnd('/') ?? "org";
                var key = $"org:{org}:projects";
                int minutes = 30;
                if (!int.TryParse(_configuration["Caching:ProjectsMinutes"], out minutes)) minutes = 30;

                return await _cache.GetOrSetAsync<List<string>>(key, async () =>
                {
                    var projects = await _projectClient.GetProjects();
                    return projects.Select(p => p.Name).ToList();
                }, TimeSpan.FromMinutes(minutes));
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
                var org = _configuration["AzureDevOps:OrganizationUrl"]?.TrimEnd('/') ?? "org";
                var key = $"org:{org}:project:{project}:workitemtypes";
                int minutes = 15;
                if (!int.TryParse(_configuration["Caching:WorkItemTypesMinutes"], out minutes)) minutes = 15;

                return await _cache.GetOrSetAsync<List<string>>(key, async () =>
                {
                    var workItemTypes = await _workItemService.GetWorkItemTypesAsync(project);
                    return workItemTypes.Select(wit => wit.Name).ToList();
                }, TimeSpan.FromMinutes(minutes));
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
                var org = _configuration["AzureDevOps:OrganizationUrl"]?.TrimEnd('/') ?? "org";
                var key = $"org:{org}:project:{project}:iterations";
                int minutes = 15;
                if (!int.TryParse(_configuration["Caching:IterationsMinutes"], out minutes)) minutes = 15;

                return await _cache.GetOrSetAsync<List<string>>(key, async () =>
                {
                    var iterations = await _workItemService.GetIterationsAsync(project);
                    return iterations.Select(i => i.Name).ToList();
                }, TimeSpan.FromMinutes(minutes));
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
            // Return metadata as PatResponse without token values (AccessToken left empty)
            return _patMetadataStore.Select(m => new PatResponse
            {
                AccessToken = string.Empty,
                DisplayName = m.DisplayName,
                Scope = m.Scope,
                ValidTo = m.ValidTo,
                AllOrgs = m.AllOrgs
            }).ToList();
        }

        public Task StorePatMetadataAsync(PatMetadata metadata)
        {
            // Store only metadata in memory. Replace with secure persistent storage in production.
            _patMetadataStore.Add(metadata);
            _logger.LogInformation("Stored PAT metadata for {DisplayName} by {CreatedBy}", metadata.DisplayName, metadata.CreatedBy);
            return Task.CompletedTask;
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

        public async Task<bool> UpdateWorkItemAsync(int workItemId, string state, string comment, string assignedUser, int? priority, double? remainingEffortHours, double? completedEffortHours, string? tag, int? rev = null)
        {
            try
            {
                var patchDocument = new Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchDocument();

                if (!string.IsNullOrEmpty(state))
                {
                    patchDocument.Add(new Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchOperation
                    {
                        Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
                        Path = "/fields/System.State",
                        Value = state
                    });
                }

                if (!string.IsNullOrEmpty(comment))
                {
                    patchDocument.Add(new Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchOperation
                    {
                        Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                        Path = "/fields/System.History",
                        Value = comment + " - Added By Service Api"
                    });
                }

                if (!string.IsNullOrEmpty(assignedUser))
                {
                    patchDocument.Add(new Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchOperation
                    {
                        Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
                        Path = "/fields/System.AssignedTo",
                        Value = assignedUser
                    });
                }

                if (priority.HasValue)
                {
                    patchDocument.Add(new Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchOperation
                    {
                        Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
                        Path = "/fields/Microsoft.VSTS.Common.Priority",
                        Value = priority
                    });
                }

                if (remainingEffortHours.HasValue && !string.Equals(state, "Closed", StringComparison.OrdinalIgnoreCase))
                {
                    patchDocument.Add(new Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchOperation
                    {
                        Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
                        Path = "/fields/Microsoft.VSTS.Scheduling.RemainingWork",
                        Value = remainingEffortHours.ToString()
                    });
                }

                if (completedEffortHours.HasValue)
                {
                    patchDocument.Add(new Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchOperation
                    {
                        Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
                        Path = "/fields/Microsoft.VSTS.Scheduling.CompletedWork",
                        Value = completedEffortHours
                    });
                }

                if (!string.IsNullOrEmpty(tag))
                {
                    patchDocument.Add(new Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchOperation
                    {
                        Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                        Path = "/fields/System.Tags",
                        Value = tag
                    });
                }

                try
                {
                    var workItem = await _workItemService.UpdateWorkItemAsync(patchDocument, workItemId, rev);
                    return workItem != null;
                }
                catch (Microsoft.VisualStudio.Services.Common.VssServiceException ex)
                {
                    // Map to 409 Conflict for concurrency errors
                    throw new AzureDevOpsApiException("Conflict updating work item - revision does not match", System.Net.HttpStatusCode.Conflict, ex.Message);
                }
            }
            catch (AzureDevOpsApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update work item {WorkItemId}", workItemId);
                throw new AzureDevOpsApiException("Failed to update work item", ex);
            }
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

        // Work Item Relations
        public async Task<bool> AddWorkItemRelationAsync(int workItemId, WorkItemRelationRequest relation)
        {
            try
            {
                var patchDocument = new JsonPatchDocument();
                patchDocument.Add(
                    new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/relations/-",
                        Value = new
                        {
                            rel = relation.RelationType,
                            url = $"{_workItemTrackingHttpClient.BaseAddress}/_apis/wit/workItems/{relation.TargetWorkItemId}",
                            attributes = new Dictionary<string, object>
                            {
                                { "comment", relation.Comment ?? string.Empty }
                            }
                        }
                    });

                var workItem = await _workItemService.UpdateWorkItemAsync(patchDocument, workItemId, null);
                return workItem != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add work item relation");
                throw new AzureDevOpsApiException("Failed to add work item relation", ex);
            }
        }

        public async Task<bool> RemoveWorkItemRelationAsync(int workItemId, int targetWorkItemId, string relationType)
        {
            try
            {
                return await _workItemService.RemoveWorkItemRelationAsync(workItemId, targetWorkItemId, relationType);
            }
            catch (Exception ex)
            {
                throw new AzureDevOpsApiException("Failed to remove work item relation", ex);
            }
        }

        public async Task<List<WorkItemRelationResponse>> GetWorkItemRelationsAsync(int workItemId)
        {
            try
            {
                return await _workItemService.GetWorkItemRelationsAsync(workItemId);
            }
            catch (Exception ex)
            {
                throw new AzureDevOpsApiException("Failed to get work item relations", ex);
            }
        }

        public async Task<List<WorkItem>> GetRelatedWorkItemsAsync(int workItemId, string relationType)
        {
            try
            {
                return await _workItemService.GetRelatedWorkItemsAsync(workItemId, relationType);
            }
            catch (Exception ex)
            {
                throw new AzureDevOpsApiException("Failed to get related work items", ex);
            }
        }

        public async Task<List<WorkItem>> GetFilteredWorkItemsAsync(WorkItemFilterRequest filter)
        {
            try
            {
                var wiql = BuildWiqlQuery(filter);
                var query = new Wiql { Query = wiql };
                
                var queryResult = await _policies.ExecuteAsync(async () =>
                    await _workItemTrackingHttpClient.QueryByWiqlAsync(query, filter.Project));

                if (queryResult.WorkItems == null || !queryResult.WorkItems.Any())
                    return new List<WorkItem>();

                var workItemIds = queryResult.WorkItems.Select(wi => wi.Id).ToArray();
                var workItems = await _policies.ExecuteAsync(async () =>
                    await _workItemTrackingHttpClient.GetWorkItemsAsync(workItemIds));

                return workItems.Select(wi => new WorkItem
                {
                    Id = wi.Id,
                    Fields = new Dictionary<string, object>
                    {
                        ["System.Title"] = wi.Fields["System.Title"]?.ToString() ?? string.Empty,
                        ["System.WorkItemType"] = wi.Fields["System.WorkItemType"]?.ToString() ?? string.Empty,
                        ["System.State"] = wi.Fields["System.State"]?.ToString() ?? string.Empty,
                        ["System.AssignedTo"] = wi.Fields["System.AssignedTo"]?.ToString() ?? string.Empty,
                        ["System.CreatedDate"] = wi.Fields["System.CreatedDate"] ?? DateTime.MinValue,
                        ["System.ChangedDate"] = wi.Fields["System.ChangedDate"] ?? DateTime.MinValue,
                        ["Microsoft.VSTS.Common.Priority"] = wi.Fields["Microsoft.VSTS.Common.Priority"] ?? 0,
                        ["Microsoft.VSTS.Scheduling.Effort"] = wi.Fields["Microsoft.VSTS.Scheduling.Effort"] ?? 0.0
                    }
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering work items");
                throw new AzureDevOpsApiException("Failed to filter work items", ex);
            }
        }

        private string BuildWiqlQuery(WorkItemFilterRequest filter)
        {
            var conditions = new List<string>();
            
            // Required project condition
            conditions.Add($"[System.TeamProject] = '{filter.Project}'");

            // Work item types
            if (filter.WorkItemTypes?.Any() == true)
            {
                var types = string.Join("', '", filter.WorkItemTypes);
                conditions.Add($"[System.WorkItemType] IN ('{types}')");
            }

            // States
            if (filter.States?.Any() == true)
            {
                var states = string.Join("', '", filter.States);
                conditions.Add($"[System.State] IN ('{states}')");
            }

            // Assigned to
            if (filter.AssignedTo?.Any() == true)
            {
                var users = string.Join("', '", filter.AssignedTo);
                conditions.Add($"[System.AssignedTo] IN ('{users}')");
            }

            // Date ranges
            if (filter.CreatedAfter.HasValue)
                conditions.Add($"[System.CreatedDate] >= '{filter.CreatedAfter.Value:yyyy-MM-dd}'");
            if (filter.CreatedBefore.HasValue)
                conditions.Add($"[System.CreatedDate] <= '{filter.CreatedBefore.Value:yyyy-MM-dd}'");
            if (filter.ChangedAfter.HasValue)
                conditions.Add($"[System.ChangedDate] >= '{filter.ChangedAfter.Value:yyyy-MM-dd}'");
            if (filter.ChangedBefore.HasValue)
                conditions.Add($"[System.ChangedDate] <= '{filter.ChangedBefore.Value:yyyy-MM-dd}'");

            // Tags
            if (filter.Tags?.Any() == true)
            {
                var tags = string.Join("' AND [System.Tags] CONTAINS '", filter.Tags);
                conditions.Add($"[System.Tags] CONTAINS '{tags}'");
            }

            // Title and description
            if (!string.IsNullOrEmpty(filter.TitleContains))
                conditions.Add($"[System.Title] CONTAINS '{filter.TitleContains}'");
            if (!string.IsNullOrEmpty(filter.DescriptionContains))
                conditions.Add($"[System.Description] CONTAINS '{filter.DescriptionContains}'");

            // Priority
            if (filter.Priority.HasValue)
                conditions.Add($"[Microsoft.VSTS.Common.Priority] = {filter.Priority.Value}");

            // Effort hours range
            if (filter.EffortHoursMin.HasValue)
                conditions.Add($"[Microsoft.VSTS.Scheduling.Effort] >= {filter.EffortHoursMin.Value}");
            if (filter.EffortHoursMax.HasValue)
                conditions.Add($"[Microsoft.VSTS.Scheduling.Effort] <= {filter.EffortHoursMax.Value}");

            return $"SELECT [System.Id] FROM WorkItems WHERE {string.Join(" AND ", conditions)}";
        }
    }
}
