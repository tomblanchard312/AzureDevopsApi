namespace ADOApi.Models
{
    public class UpdateWorkItemModel
    {
        public string? State { get; set; }
        public string? Comment { get; set; }
        public int? Priority { get; set; }
        public string? AssignedTo { get; set; }
        public double? RemainingEffortHours { get; set; }
        public double? CompletedEffortHours { get; set; }
        public string? Tag { get; set; }
    }
}
