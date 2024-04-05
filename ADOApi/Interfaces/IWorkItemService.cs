namespace ADOApi.Interfaces
{
    public interface IWorkItemService
    {
        Task<int> AddWorkItemAsync(string project, string workItemType, string title, string description, string assignedTo, string tag, double? effortHours, string comments, int? parentId, HttpClient httpClient, string organization, string personalAccessToken);
        Task<bool> UpdateWorkItemAsync(int workItemId, string state, string comment, string assignedUser, Nullable<int> priority, Nullable<double> remainingEffortHours, Nullable<double> completedEffortHours, HttpClient httpClient, string organization, string personalAccessToken, string tag);
    }
}
