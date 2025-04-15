using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using ADOApi.Models;

namespace ADOApi.Interfaces
{
    public interface IWorkItemService
    {
        Task<int> AddWorkItemAsync(string project, string workItemType, string title, string description, string assignedTo, string tag, double? effortHours, string comments, int? parentId, HttpClient httpClient, string organization, string personalAccessToken);
        Task<bool> UpdateWorkItemAsync(int workItemId, string state, string comment, string assignedUser, Nullable<int> priority, Nullable<double> remainingEffortHours, Nullable<double> completedEffortHours, HttpClient httpClient, string organization, string personalAccessToken, string tag);
        Task<WorkItem> CreateWorkItemAsync(JsonPatchDocument patchDocument, string project, string workItemType);
        Task<WorkItem> UpdateWorkItemAsync(JsonPatchDocument patchDocument, int workItemId);
        Task<WorkItem> GetWorkItemAsync(int id, WorkItemExpand? expand = null);
        Task<List<WorkItem>> GetWorkItemsAsync(IEnumerable<int> ids, WorkItemExpand? expand = null);
        Task<WorkItemQueryResult> QueryByWiqlAsync(Wiql query, string project);
        Task DeleteWorkItemAsync(int id);
        Task<List<WorkItemType>> GetWorkItemTypesAsync(string project);
        Task<List<GitRepository>> GetIterationsAsync(string project);

        // Work Item Relations
        Task<bool> AddWorkItemRelationAsync(int workItemId, WorkItemRelationRequest relation);
        Task<bool> RemoveWorkItemRelationAsync(int workItemId, int targetWorkItemId, string relationType);
        Task<List<WorkItemRelationResponse>> GetWorkItemRelationsAsync(int workItemId);
        Task<List<WorkItem>> GetRelatedWorkItemsAsync(int workItemId, string relationType);
    }
}
