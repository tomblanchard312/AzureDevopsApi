using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using ADOApi.Exceptions;
using ADOApi.Interfaces;
using ADOApi.Models;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace ADOApi.Services
{
    public class WorkItemService : IWorkItemService
    {
        private readonly WorkItemTrackingHttpClient _workItemTrackingHttpClient;
        private readonly GitHttpClient _gitHttpClient;
        private readonly ILogger<WorkItemService> _logger;

        public WorkItemService(
            WorkItemTrackingHttpClient workItemTrackingHttpClient,
            GitHttpClient gitHttpClient,
            ILogger<WorkItemService> logger)
        {
            _workItemTrackingHttpClient = workItemTrackingHttpClient;
            _gitHttpClient = gitHttpClient;
            _logger = logger;
        }

        public async Task<WorkItem> CreateWorkItemAsync(JsonPatchDocument patchDocument, string project, string workItemType)
        {
            try
            {
                return await _workItemTrackingHttpClient.CreateWorkItemAsync(patchDocument, project, workItemType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create work item");
                throw;
            }
        }

        public async Task<WorkItem> GetWorkItemAsync(int id, WorkItemExpand? expand = null)
        {
            try
            {
                return await _workItemTrackingHttpClient.GetWorkItemAsync(id, expand: expand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get work item");
                throw;
            }
        }

        public async Task<List<WorkItem>> GetWorkItemsAsync(IEnumerable<int> ids, WorkItemExpand? expand = null)
        {
            try
            {
                var workItems = await _workItemTrackingHttpClient.GetWorkItemsAsync(ids, expand: expand);
                return workItems.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get work items");
                throw;
            }
        }

        public async Task<WorkItemQueryResult> QueryByWiqlAsync(Wiql query, string project)
        {
            try
            {
                return await _workItemTrackingHttpClient.QueryByWiqlAsync(query, project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query work items");
                throw;
            }
        }

        public async Task DeleteWorkItemAsync(int id)
        {
            try
            {
                await _workItemTrackingHttpClient.DeleteWorkItemAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete work item");
                throw;
            }
        }

        public async Task<int> AddWorkItemAsync(string project, string workItemType, string title, string description, string assignedTo, string tag, double? effortHours, string comments, int? parentId, HttpClient httpClient, string organization, string personalAccessToken)
        {
            try
            {
                var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
                var connection = new VssConnection(new Uri($"https://dev.azure.com/{organization}"), credentials);
                var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

                JsonPatchDocument patchDocument =
                [
                    // Add fields to the patch document
                    new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Title",
                        Value = title
                    },
                    new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.IterationPath",
                        Value = $"{project}\\Iteration 1" // Adjust the iteration path as needed
                    },
                    new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.WorkItemType",
                        Value = workItemType
                    },
                ];

                if (!string.IsNullOrEmpty(description))
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Description",
                        Value = description
                    });
                }

                if (!string.IsNullOrEmpty(assignedTo))
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.AssignedTo",
                        Value = assignedTo
                    });
                }

                if (!string.IsNullOrEmpty(tag))
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Tags",
                        Value = tag
                    });
                }

                if (effortHours.HasValue)
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/Microsoft.VSTS.Scheduling.OriginalEstimate",
                        Value = effortHours.ToString() // Assuming effort hours are represented as a string
                    });
                }

                if (!string.IsNullOrEmpty(comments))
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.History",
                        Value = comments
                    });
                }

                if (parentId.HasValue)
                {
                    string baseUrl = $"https://dev.azure.com/{organization}/";
                    string parentUrl = $"{baseUrl}{project}/_apis/wit/workitems/{parentId}";
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/relations/-",
                        Value = new
                        {
                            rel = "System.LinkTypes.Hierarchy-Reverse",
                            url = parentUrl,
                            attributes = new { comment = "Parent-Child relation" }
                        }
                    });
                }

                WorkItem result = await _workItemTrackingHttpClient.CreateWorkItemAsync(patchDocument, project, workItemType);
                return result.Id ?? 0;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new AzureDevOpsApiException($"Failed to create work item: {ex.Message}", ex);
            }
        }
        public async Task<bool> UpdateWorkItemAsync(
            int workItemId,
            string state,
            string comment,
            string assignedUser,
            Nullable<int> priority,
            Nullable<double> remainingEffortHours,
            Nullable<double> completedEffortHours,
            HttpClient httpClient,
            string organization,
            string personalAccessToken, string tag)
        {
            try
            {
                var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
                var connection = new VssConnection(new Uri($"https://dev.azure.com/{organization}"), credentials);
                var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

                JsonPatchDocument patchDocument = [];

                if (!string.IsNullOrEmpty(state))
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Replace,
                        Path = "/fields/System.State",
                        Value = state
                    });
                }

                if (!string.IsNullOrEmpty(comment))
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.History",
                        Value = comment + " - Added By Service Api"
                    });
                }

                if (!string.IsNullOrEmpty(assignedUser))
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Replace,
                        Path = "/fields/System.AssignedTo",
                        Value = assignedUser
                    });
                }

                if (priority.HasValue)
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Replace,
                        Path = "/fields/Microsoft.VSTS.Common.Priority",
                        Value = priority
                    });
                }

                if (remainingEffortHours.HasValue && !string.Equals(state, "Closed", StringComparison.OrdinalIgnoreCase))
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Replace,
                        Path = "/fields/Microsoft.VSTS.Scheduling.RemainingWork",
                        Value = remainingEffortHours.ToString() // Convert to string if necessary
                    });
                }

                if (completedEffortHours.HasValue)
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Replace,
                        Path = "/fields/Microsoft.VSTS.Scheduling.CompletedWork",
                        Value = completedEffortHours
                    });
                }
                if (!string.IsNullOrEmpty(tag))
                {
                    patchDocument.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Tags",
                        Value = tag
                    });
                }
                WorkItem result = await _workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, workItemId);
                return result != null;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new AzureDevOpsApiException($"Failed to update work item: {ex.Message}", ex);
            }
        }

        public async Task<List<WorkItemType>> GetWorkItemTypesAsync(string project)
        {
            try
            {
                var workItemTypes = await _workItemTrackingHttpClient.GetWorkItemTypesAsync(project);
                return workItemTypes.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get work item types");
                throw;
            }
        }

        public async Task<List<GitRepository>> GetIterationsAsync(string project)
        {
            try
            {
                var repositories = await _gitHttpClient.GetRepositoriesAsync(project);
                return repositories.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get iterations");
                throw;
            }
        }

        // Work Item Relations Implementation
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
                            url = $"https://dev.azure.com/{_workItemTrackingHttpClient.BaseAddress.Host}/_apis/wit/workItems/{relation.TargetWorkItemId}",
                            attributes = new Dictionary<string, object>
                            {
                                { "comment", relation.Comment ?? string.Empty }
                            }
                        }
                    });

                var workItem = await _workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, workItemId);
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
                var workItem = await _workItemTrackingHttpClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations);
                if (workItem?.Relations == null)
                {
                    return false;
                }

                var relationToRemove = workItem.Relations.FirstOrDefault(r => 
                    r.Rel == relationType && 
                    r.Url.EndsWith($"/{targetWorkItemId}"));

                if (relationToRemove == null)
                {
                    return false;
                }

                var patchDocument = new JsonPatchDocument();
                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Remove,
                        Path = $"/relations/{workItem.Relations.IndexOf(relationToRemove)}"
                    }
                );

                var result = await _workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, workItemId);
                return result != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove work item relation");
                throw;
            }
        }

        public async Task<List<WorkItemRelationResponse>> GetWorkItemRelationsAsync(int workItemId)
        {
            try
            {
                var workItem = await _workItemTrackingHttpClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations);
                if (workItem?.Relations == null)
                {
                    return new List<WorkItemRelationResponse>();
                }

                return workItem.Relations.Select(r => new WorkItemRelationResponse
                {
                    RelationType = r.Rel,
                    SourceWorkItemId = workItemId,
                    TargetWorkItemId = int.Parse(r.Url.Split('/').Last()),
                    Comment = r.Attributes?["comment"]?.ToString(),
                    CreatedDate = r.Attributes?["createdDate"] != null ? 
                        DateTime.Parse(r.Attributes["createdDate"].ToString()) : DateTime.MinValue,
                    CreatedBy = r.Attributes?["createdBy"]?.ToString() ?? string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get work item relations");
                throw;
            }
        }

        public async Task<List<WorkItem>> GetRelatedWorkItemsAsync(int workItemId, string relationType)
        {
            try
            {
                var workItem = await _workItemTrackingHttpClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations);
                if (workItem?.Relations == null)
                {
                    return new List<WorkItem>();
                }

                var relatedWorkItemIds = workItem.Relations
                    .Where(r => r.Rel == relationType)
                    .Select(r => int.Parse(r.Url.Split('/').Last()))
                    .ToList();

                if (!relatedWorkItemIds.Any())
                {
                    return new List<WorkItem>();
                }

                return await _workItemTrackingHttpClient.GetWorkItemsAsync(relatedWorkItemIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get related work items");
                throw;
            }
        }

        public async Task<WorkItem> UpdateWorkItemAsync(JsonPatchDocument patchDocument, int workItemId, int? rev = null)
        {
            try
            {
                if (rev.HasValue)
                {
                    var current = await _workItemTrackingHttpClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.None);
                    if (current == null)
                    {
                        throw new AzureDevOpsApiException($"Work item {workItemId} not found");
                    }

                    if (current.Rev != rev.Value)
                    {
                        throw new AzureDevOpsApiException("Revision mismatch", System.Net.HttpStatusCode.Conflict, "Revision does not match current work item revision");
                    }
                }

                return await _workItemTrackingHttpClient.UpdateWorkItemAsync(patchDocument, workItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update work item");
                throw new AzureDevOpsApiException("Failed to update work item", ex);
            }
        }
    }
}