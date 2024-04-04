using System.Net.Http.Headers;

using ADOApi.Models;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using Newtonsoft.Json.Linq;

namespace ADOApi.Interfaces
{
    public interface IQueryService
    {
        Task<List<string>> GetWorkItemTypesAsync(string project, HttpClient httpClient, string organization, string personalAccessToken);
        Task<List<string>> GetIterationsAsync(string project, HttpClient httpClient, string organization, string personalAccessToken);
        Task<List<WorkItem>> GetWorkItemsByTypeAsync(string project, string workItemType, HttpClient httpClient, string organization, string personalAccessToken);
        Task<List<RecentWorkItem>> GetRecentWorkItemsAsync(HttpClient httpClient, string organization, string personalAccessToken);
        Task<WorkItem> GetWorkItemByIdAsync(int workItemId, string project, HttpClient httpClient, string organization, string personalAccessToken);
        Task<List<string>> GetProjectsAsync(HttpClient httpClient, string organization, string personalAccessToken);
    }
}
