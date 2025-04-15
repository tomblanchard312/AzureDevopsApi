using System.Collections.Generic;

namespace ADOApi.Models
{
    public class WorkItemTemplate
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string WorkItemType { get; set; }
        public required string Project { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? AssignedTo { get; set; }
        public string? State { get; set; }
        public int? Priority { get; set; }
        public double? EffortHours { get; set; }
        public string? Tags { get; set; }
        public Dictionary<string, object>? CustomFields { get; set; }
    }
} 