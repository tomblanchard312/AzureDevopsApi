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

namespace ADOApi.Services
{
    public class AzureDevOpsService : IAzureDevOpsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _organization;
        private readonly string _personalAccessToken;
        private readonly string _adminToken;
        private readonly IAuthenticationService _authService;
        private readonly IQueryService _queryService;
        private readonly IWorkItemService _workItemService;

        public AzureDevOpsService(
            HttpClient httpClient,
            string organization,
            string personalAccessToken,
            string adminToken,
            IAuthenticationService authService,
            IQueryService queryService,
            IWorkItemService workItemService)
        {
            _httpClient = httpClient;
            _organization = organization;
            _personalAccessToken = personalAccessToken;
            _adminToken = adminToken;
            _authService = authService;
            _queryService = queryService;
            _workItemService = workItemService;
        }

        public async Task<List<string>> GetWorkItemTypesAsync(string project)
        {
            return await _queryService.GetWorkItemTypesAsync(project, _httpClient, _organization, _personalAccessToken);
        }

        public async Task<List<string>> GetIterationsAsync(string project)
        {
            return await _queryService.GetIterationsAsync(project, _httpClient, _organization, _personalAccessToken);
        }
        public async Task<List<string>> GetProjectsAsync()
        {
            return await _queryService.GetProjectsAsync(_httpClient, _organization, _personalAccessToken);
        }

        public async Task<List<WorkItem>> GetWorkItemsByTypeAsync(string project, string workItemType) {
            return await _queryService.GetWorkItemsByTypeAsync(project, workItemType, _httpClient, _organization, _personalAccessToken);
        }
        public async Task<string> CreatePersonalAccessTokenAsync(string displayName, string scope, DateTime validTo, bool allOrgs = false)
        {
            return await _authService.CreatePersonalAccessTokenAsync(displayName, scope, validTo, allOrgs, _httpClient, _organization, _adminToken);
        }

        public async Task<List<PatResponse>> GetTokensAsync()
        {
            return await _authService.GetTokensAsync(_httpClient, _organization, _adminToken);
        }

        public Task<int> AddWorkItemAsync(string project, string workItemType, string title, string description, string assignedTo, string tag, double? effortHours = null, string comments = null, int? parentId = null)
        {
            return _workItemService.AddWorkItemAsync(project, workItemType, title, description, assignedTo, tag, effortHours, comments, parentId, _httpClient, _organization, _personalAccessToken);
        }

        public async Task<List<RecentWorkItem>> GetRecentWorkItemsAsync()
        {
            return await _queryService.GetRecentWorkItemsAsync(_httpClient, _organization, _personalAccessToken);
        }
        public async Task<WorkItem> GetWorkItemByIdAsync(int workItemId, string project)
        {
            return await _queryService.GetWorkItemByIdAsync(workItemId, project, _httpClient, _organization, _personalAccessToken);
        }

        public async Task<bool> UpdateWorkItemAsync(
        int workItemId,
        string state,
        string comment,
            string assignedUser,
            Nullable<int> priority,
            Nullable<double> remainingEffortHours,
            Nullable<double> completedEffortHours,
            string? tag)
        {
            return await _workItemService.UpdateWorkItemAsync(workItemId, state, comment, assignedUser, priority, remainingEffortHours, completedEffortHours, _httpClient, _organization, _personalAccessToken, tag);
        }
        /// <summary>
        /// Retrieves all work items for a specified project.
        /// </summary>
        /// <param name="project">The project name to retrieve work items from.</param>
        /// <returns>A list of work items.</returns>
        public async Task<List<WorkItem>> GetAllWorkItemsForProjectAsync(string project)
        {
            return await _queryService.GetAllWorkItemsForProjectAsync(project, _httpClient, _organization, _personalAccessToken);
        }
        public async Task<List<WorkItem>> GetMyAssignedWorkItemsAsync(string project, string userIdentifier) {
            return await _queryService.GetMyAssignedWorkItemsAsync(project, _httpClient, _organization, _personalAccessToken, userIdentifier);
        }

        public Task<bool> UpdateWorkItemAsync(int workItemId, string state, string comment, string assignedUser, int? priority, double? remainingEffortHours, double? completedEffortHours)
        {
            throw new NotImplementedException();
        }
    }
}
