using System.ComponentModel.DataAnnotations;

namespace ADOApi.Models.Chat
{
    public class RepoChatResponse
    {
        public string Reply { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<ChatSource> Sources { get; set; } = new List<ChatSource>();
        public List<WorkItemProposalDraft> Proposals { get; set; } = new List<WorkItemProposalDraft>();
        public string? Notes { get; set; }
    }

    public class ChatSource
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class WorkItemProposalDraft
    {
        public string WorkItemType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] AcceptanceCriteria { get; set; } = Array.Empty<string>();
        public double Confidence { get; set; }
        public string Rationale { get; set; } = string.Empty;
    }
}