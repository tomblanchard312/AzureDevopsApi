using System.ComponentModel.DataAnnotations;

namespace ADOApi.Models.Chat
{
    public class ChatContext
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public Dictionary<string, string> RepositoryFiles { get; set; } = new Dictionary<string, string>();
        public List<string> RecentCommits { get; set; } = new List<string>();
        public List<string> ActiveWorkItems { get; set; } = new List<string>();
    }
}