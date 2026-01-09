using ADOApi.Models;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOApi.Interfaces
{
    public interface IAzureDevOpsService
    {
        Task<List<string>> GetProjectsAsync();
        Task<List<string>> GetWorkItemTypesAsync(string project);
        Task<List<string>> GetIterationsAsync(string project);
        Task<List<WorkItem>> GetWorkItemsByTypeAsync(string project, string workItemType);
        Task<string> CreatePersonalAccessTokenAsync(string displayName, string scope, DateTime validTo, bool allOrgs = false);
        Task StorePatMetadataAsync(ADOApi.Models.PatMetadata metadata);
        Task<List<PatResponse>> GetTokensAsync();
        Task<int> AddWorkItemAsync(string project, string workItemType, string title, string description, string assignedTo, string tag, double? effortHours = null, string? comments = null, int? parentId = null);
        Task<List<RecentWorkItem>> GetRecentWorkItemsAsync();
        Task<bool> UpdateWorkItemAsync(
            int workItemId,
            string state,
            string comment,
            string assignedUser,
            int? priority,
            double? remainingEffortHours,
            double? completedEffortHours, string? tag,
            int? rev = null);
        Task<List<WorkItem>> GetAllWorkItemsForProjectAsync(string project);
        Task<List<WorkItem>> GetMyAssignedWorkItemsAsync(string project, string userIdentifier);
        Task<WorkItem> GetWorkItemByIdAsync(int workItemId, string project);
        Task<int> CreateWorkItemTemplateAsync(ADOApi.Models.WorkItemTemplate template);
        Task<List<ADOApi.Models.WorkItemTemplate>> GetWorkItemTemplatesAsync(string project);
        Task<int> CreateWorkItemFromTemplateAsync(int templateId, Dictionary<string, object>? overrides = null);
        Task DeleteWorkItemTemplateAsync(int templateId);
        Task<bool> AddWorkItemRelationAsync(int workItemId, WorkItemRelationRequest relation);
        Task<bool> RemoveWorkItemRelationAsync(int workItemId, int targetWorkItemId, string relationType);
        Task<List<WorkItemRelationResponse>> GetWorkItemRelationsAsync(int workItemId);
        Task<List<WorkItem>> GetRelatedWorkItemsAsync(int workItemId, string relationType);
        Task<List<WorkItem>> GetFilteredWorkItemsAsync(WorkItemFilterRequest filter);
    }
}