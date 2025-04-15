using ADOApi.Models;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOApi.Interfaces
{
    public interface IAzureDevOpsService
    {
        Task<List<string>> GetWorkItemTypesAsync(string project);
        Task<List<string>> GetIterationsAsync(string project);
        Task<List<WorkItem>> GetWorkItemsByTypeAsync(string project, string workItemType);
        Task<string> CreatePersonalAccessTokenAsync(string displayName, string scope, DateTime validTo, bool allOrgs = false);
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
            double? completedEffortHours, string? tag);
    }
}