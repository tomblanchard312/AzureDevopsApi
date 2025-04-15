using System;

namespace ADOApi.Models
{
    public class WorkItemRelationRequest
    {
        public string RelationType { get; set; } = string.Empty;
        public int TargetWorkItemId { get; set; }
        public string? Comment { get; set; }
    }

    public class WorkItemRelationResponse
    {
        public string RelationType { get; set; } = string.Empty;
        public int SourceWorkItemId { get; set; }
        public int TargetWorkItemId { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public static class WorkItemRelationTypes
    {
        public const string Parent = "System.LinkTypes.Hierarchy-Reverse";
        public const string Child = "System.LinkTypes.Hierarchy-Forward";
        public const string Related = "System.LinkTypes.Related";
        public const string Duplicate = "System.LinkTypes.Duplicate-Forward";
        public const string DuplicateOf = "System.LinkTypes.Duplicate-Reverse";
        public const string Blocks = "System.LinkTypes.Dependency-Forward";
        public const string BlockedBy = "System.LinkTypes.Dependency-Reverse";
    }
} 