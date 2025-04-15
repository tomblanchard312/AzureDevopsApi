using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using ADOApi.Exceptions;
using ADOApi.Interfaces;
using ADOApi.Models;
using ADOApi.Utilities;
using Serilog;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Log = Serilog.Log;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Organization.Client;
using Microsoft.Extensions.Logging;

namespace ADOApi.Services
{
    public class QueryService : IQueryService
    {
        private readonly WorkItemTrackingHttpClient _workItemTrackingHttpClient;
        private readonly ILogger<QueryService> _logger;

        public QueryService(WorkItemTrackingHttpClient workItemTrackingHttpClient, ILogger<QueryService> logger)
        {
            _workItemTrackingHttpClient = workItemTrackingHttpClient;
            _logger = logger;
        }

        public async Task<WorkItem> GetWorkItemByIdAsync(int workItemId, string project, HttpClient httpClient, string organization, string personalAccessToken)
        {
            try
            {
                return await _workItemTrackingHttpClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get work item");
                throw;
            }
        }

        public async Task<List<string>> GetWorkItemTypesAsync(string project, HttpClient httpClient, string organization, string personalAccessToken)
        {
            try
            {
                string requestUri = $"{project}/_apis/wit/workitemtypes?api-version=7.0";
                string baseUrl = $"https://dev.azure.com/{organization}/";
                httpClient.BaseAddress = new Uri(baseUrl);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}")));

                var response = await httpClient.GetAsync(baseUrl + requestUri);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var workItemTypes = JsonSerializer.Deserialize<JsonElement>(responseBody);

                List<string> workItemTypeNames = [];
                foreach (var workItemType in workItemTypes.GetProperty("value").EnumerateArray())
                {
                    var name = workItemType.GetProperty("name").GetString();
                    if (!string.IsNullOrEmpty(name)) // Ensure the name is not null or empty
                    {
                        workItemTypeNames.Add(name);
                    }
                }

                return workItemTypeNames;
            }
            catch (HttpRequestException ex)
            {
                Log.Error(ex, "Error occurred while retrieving work item types.");
                throw new AzureDevOpsApiException("Failed to retrieve work item types.", ex);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred while retrieving work item types.");
                throw new AzureDevOpsApiException("An unexpected error occurred while retrieving work item types.", ex);
            }
        }

        public async Task<List<string>> GetIterationsAsync(string project, HttpClient httpClient, string organization, string personalAccessToken)
        {
            try
            {
                var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
                var connection = new VssConnection(new Uri($"https://dev.azure.com/{organization}"), credentials);
                var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

                string wiqlQuery = $"SELECT [System.IterationPath] FROM WorkItems WHERE [System.TeamProject] = '{project}'";

                Wiql query = new Wiql() { Query = wiqlQuery };
                WorkItemQueryResult queryResult = await witClient.QueryByWiqlAsync(query, project);

                List<string> iterationPaths = [];
                foreach (var workItemReference in queryResult.WorkItems)
                {
                    WorkItem workItem = await witClient.GetWorkItemAsync(workItemReference.Id);
                    if (workItem.Fields.TryGetValue("System.IterationPath", out object iterationPathObj) && iterationPathObj != null)
                    {
                        string iterationPath = iterationPathObj.ToString();
                        iterationPaths.Add(Uri.EscapeDataString(iterationPath));
                    }
                }

                List<string> iterations = [.. iterationPaths.Distinct()];
                return iterations;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception($"Failed to retrieve iterations: {ex.Message}", ex);
            }
        }


        public async Task<List<WorkItem>> GetWorkItemsByTypeAsync(string project, string workItemType, HttpClient httpClient, string organization, string personalAccessToken)
        {
            try
            {
                var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
                var connection = new VssConnection(new Uri($"https://dev.azure.com/{organization}"), credentials);
                var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

                Wiql wiql = new Wiql
                {
                    Query = $"SELECT [System.Id], [System.Title], [System.State] FROM WorkItems WHERE [System.TeamProject] = '{project}' AND [System.WorkItemType] = '{workItemType}'"
                };

                WorkItemQueryResult queryResult = await witClient.QueryByWiqlAsync(wiql);

                if (queryResult == null || queryResult.WorkItems == null)
                {
                    return [];
                }

                List<WorkItem> workItems = [];

                foreach (var workItemReference in queryResult.WorkItems)
                {
                    WorkItem workItem = await witClient.GetWorkItemAsync(workItemReference.Id, expand: WorkItemExpand.All);
                    workItems.Add(workItem);
                }

                return workItems;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new AzureDevOpsApiException($"Failed to retrieve work items by type: {ex.Message}", ex);
            }
        }
        public async Task<List<RecentWorkItem>> GetRecentWorkItemsAsync(HttpClient httpClient, string organization, string personalAccessToken)
        {
            try
            {
                string requestUrl = $"https://dev.azure.com/{organization}/_apis/work/accountmyworkrecentactivity?api-version=7.0";
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}")));

                var response = await httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var recentItems = JsonSerializer.Deserialize<List<RecentWorkItemResponse>>(content);

                if (recentItems == null)
                {
                    return new List<RecentWorkItem>();
                }

                return recentItems.Select(item => new RecentWorkItem
                {
                    Id = item.Id,
                    Title = item.Title ?? string.Empty,
                    State = item.State ?? string.Empty,
                    WorkItemType = item.WorkItemType ?? string.Empty,
                    AssignedTo = item.AssignedTo ?? string.Empty,
                    ChangedDate = item.ChangedDate,
                    IdentityId = item.IdentityId ?? string.Empty,
                    ActivityDate = item.ActivityDate,
                    ActivityType = item.ActivityType ?? string.Empty,
                    TeamProject = item.TeamProject ?? string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent work items");
                throw new AzureDevOpsApiException("Failed to get recent work items", ex);
            }
        }
        private static List<RecentWorkItem> DeserializeRecentWorkItems(string json)
        {
            try
            {
                // Remove escape characters from the JSON string
                string unescapedJson = JsonHelper.Unescape(json);

                // Parse the JSON document
                using (JsonDocument document = JsonDocument.Parse(unescapedJson))
                {
                    // Get the root element
                    JsonElement root = document.RootElement;

                    // Get the "value" array
                    JsonElement valueArray = root.GetProperty("value");

                    // Deserialize each item in the "value" array into a RecentWorkItem object
                    List<RecentWorkItem> recentWorkItems = [];
                    foreach (JsonElement item in valueArray.EnumerateArray())
                    {
                        RecentWorkItem recentWorkItem = DeserializeRecentWorkItem(item);
                        recentWorkItems.Add(recentWorkItem);
                    }

                    return recentWorkItems;
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception($"Failed to deserialize recent work items: {ex.Message}", ex);
            }
        }

        private static RecentWorkItem DeserializeRecentWorkItem(JsonElement item)
        {
            try
            {
                // Deserialize the properties of the RecentWorkItem
                RecentWorkItem recentWorkItem = new RecentWorkItem
                {
                    ActivityDate = item.GetProperty("activityDate").GetDateTime(),
                    ActivityType = item.GetProperty("activityType").GetString(),
                    AssignedTo = item.GetProperty("assignedTo").GetProperty("name").GetString(),
                    ChangedDate = item.GetProperty("changedDate").GetDateTime(),
                    IdentityId = item.TryGetProperty("identityId", out var identityIdElement) && identityIdElement.ValueKind != JsonValueKind.Null
                        ? identityIdElement.GetString() ?? string.Empty // Ensure non-null assignment
                        : string.Empty, // Provide a default value to avoid null assignment
                    State = item.TryGetProperty("state", out var stateElement) && stateElement.ValueKind != JsonValueKind.Null
                        ? stateElement.GetString() ?? string.Empty // Ensure non-null assignment
                        : string.Empty, // Provide a default value to avoid null assignment
                    TeamProject = item.TryGetProperty("teamProject", out var teamProjectElement) && teamProjectElement.ValueKind != JsonValueKind.Null
                        ? teamProjectElement.GetString() ?? string.Empty // Ensure non-null assignment
                        : string.Empty, // Provide a default value to avoid null assignment
                    Title = item.TryGetProperty("title", out var titleElement) && titleElement.ValueKind != JsonValueKind.Null
                        ? titleElement.GetString() ?? string.Empty // Ensure non-null assignment
                        : string.Empty, // Provide a default value to avoid null assignment
                    WorkItemType = item.TryGetProperty("resource", out var resourceElement) && resourceElement.ValueKind != JsonValueKind.Null &&
                                    resourceElement.TryGetProperty("type", out var typeElement) && typeElement.ValueKind != JsonValueKind.Null
                        ? typeElement.GetString() ?? string.Empty // Ensure non-null assignment
                        : string.Empty, // Provide a default value to avoid null assignment
                    WorkItemTitle = item.GetProperty("resource").GetProperty("name").GetString(),
                    WorkItemUrl = item.GetProperty("resource").GetProperty("url").GetString()
                };

                return recentWorkItem;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception($"Failed to deserialize recent work item: {ex.Message}", ex);
            }
        }

        private static async Task<List<WorkItemDetails>> GetRelatedWorkItems(WorkItemTrackingHttpClient witClient, WorkItem workItem, string relationType)
        {
            try
            {
                List<WorkItemDetails> relatedWorkItems = [];

                if (workItem.Relations != null)
                {
                    foreach (var relation in workItem.Relations)
                    {
                        // Check if the relation is of the desired type
                        if (relation.Rel.EndsWith(relationType, StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(relation.Url.Split('/').Last(), out int relatedWorkItemId))
                            {
                                WorkItemDetails relatedWorkItem = await GetWorkItemDetails(witClient, relatedWorkItemId);
                                relatedWorkItems.Add(relatedWorkItem);
                            }
                        }
                    }
                }

                return relatedWorkItems;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception($"Failed to retrieve related work items: {ex.Message}", ex);
            }
        }

        private static async Task<WorkItemDetails> GetWorkItemDetails(WorkItemTrackingHttpClient witClient, int workItemId)
        {
            try
            {
                WorkItem workItem = await witClient.GetWorkItemAsync(workItemId);
                string? iterationPath = null; // Use nullable string to handle potential null values

                if (workItem.Fields.TryGetValue("System.IterationPath", out object? iterationPathObj) && iterationPathObj != null)
                {
                    iterationPath = iterationPathObj.ToString();
                }

                return new WorkItemDetails
                {
                    Id = workItem.Id ?? throw new InvalidOperationException("WorkItem ID is null."),
                    Title = workItem.Fields["System.Title"]?.ToString(),
                    IterationPath = iterationPath // Assign nullable string directly
                };
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception($"Failed to retrieve work item details: {ex.Message}", ex);
            }
        }
        public async Task<List<string>> GetProjectsAsync(HttpClient httpClient, string organization, string _personalAccessToken)
        {
            var projects = new List<string>();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes($":{_personalAccessToken}")));
                var url = $"https://dev.azure.com/{organization}/_apis/projects?api-version=7.0";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonResponse = JToken.Parse(content);

                foreach (var project in jsonResponse["value"])
                {
                    projects.Add(project["name"].ToString());
                }
            }

            return projects;
        }

        public async Task<List<WorkItem>> GetAllWorkItemsForProjectAsync(string project, HttpClient httpClient, string organization, string personalAccessToken)
        {
            var uri = new Uri($"https://dev.azure.com/{organization}");
            var personalAccessTokenAuth = new VssBasicCredential(string.Empty, personalAccessToken);

            using (var workItemTrackingHttpClient = new WorkItemTrackingHttpClient(uri, personalAccessTokenAuth))
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}")));

                // WIQL query to get all work items for a project
                var wiql = new Wiql()
                {
                    Query = $"Select [Id], [Title], [State], [Assigned To], [Area Path] From WorkItems Where [System.TeamProject] = '{project}'"
                };

                var result = await workItemTrackingHttpClient.QueryByWiqlAsync(wiql).ConfigureAwait(false);

                if (result.WorkItems.Any())
                {
                    // Fetching details of all work items in batches
                    var ids = result.WorkItems.Select(item => item.Id).ToArray();
                    var workItems = await workItemTrackingHttpClient.GetWorkItemsAsync(ids, expand: WorkItemExpand.All).ConfigureAwait(false);

                    return workItems;
                }
            }

            return [];
        }
        public async Task<List<WorkItem>> GetMyAssignedWorkItemsAsync(string project, HttpClient httpClient, string organization, string personalAccessToken, string userIdentifier)
        {
            var uri = new Uri($"https://dev.azure.com/{organization}");
            var personalAccessTokenAuth = new VssBasicCredential(string.Empty, personalAccessToken);

            using (var workItemTrackingHttpClient = new WorkItemTrackingHttpClient(uri, personalAccessTokenAuth))
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}")));

                // WIQL query to get work items assigned to the specific user
                var wiql = new Wiql()
                {
                    Query = $"Select [Id], [Title], [State], [Assigned To], [Area Path] From WorkItems Where [System.TeamProject] = '{project}' AND [System.AssignedTo] = '{userIdentifier}'"
                };

                var result = await workItemTrackingHttpClient.QueryByWiqlAsync(wiql).ConfigureAwait(false);

                if (result.WorkItems.Any())
                {
                    var ids = result.WorkItems.Select(item => item.Id).ToArray();
                    var workItems = await workItemTrackingHttpClient.GetWorkItemsAsync(ids, expand: WorkItemExpand.All).ConfigureAwait(false);

                    return workItems;
                }
            }

            return [];
        }
    }

    internal class RecentWorkItemResponse
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? State { get; set; }
        public string? WorkItemType { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime ChangedDate { get; set; }
        public string? IdentityId { get; set; }
        public DateTime ActivityDate { get; set; }
        public string? ActivityType { get; set; }
        public string? TeamProject { get; set; }
    }
}