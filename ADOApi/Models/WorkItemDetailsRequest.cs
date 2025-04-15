namespace ADOApi.Models
{
    public class WorkItemDetailsRequest
    {
        public required string Project { get; set; }
        public string? WorkItemType { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? AssignedTo { get; set; }
        public string? Tag { get; set; }
        public double? EffortHours { get; set; }
        public string? Comments { get; set; }
        public int? ParentWorkItemId { get; set; }
    }
}
