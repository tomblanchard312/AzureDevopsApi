using System.ComponentModel.DataAnnotations;

namespace ADOApi.Models.Chat
{
    public class RepoChatRequest
    {
        [Required]
        public string RepoKey { get; set; } = string.Empty;

        [Required]
        public string Mode { get; set; } = string.Empty; // Explore | Review | Plan | MemoryDraft

        [Required]
        public string Message { get; set; } = string.Empty;

        public RepoChatContext Context { get; set; } = new RepoChatContext();
    }

    public class RepoChatContext
    {
        public string[] FilePaths { get; set; } = Array.Empty<string>();
        public string? CommitSha { get; set; }
        public Guid[] InsightIds { get; set; } = Array.Empty<Guid>();
    }
}