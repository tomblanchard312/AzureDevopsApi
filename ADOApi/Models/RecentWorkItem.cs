namespace ADOApi.Models
{
    public class RecentWorkItem
    {
        public string ActivityDate { get; set; }
        public String ActivityType { get; set; }
        public IdentityRef AssignedTo { get; set; }
        public string ChangedDate { get; set; }
        public int Id { get; set; }
        public string IdentityId { get; set; }
        public string State { get; set; }
        public string TeamProject { get; set; }
        public string Title { get; set; }
        public string WorkItemType { get; set; }
        public string? ProjectName { get; internal set; }
        public int WorkItemId { get; internal set; }
        public string? WorkItemTitle { get; internal set; }
        public string? WorkItemUrl { get; internal set; }
    }

    public class IdentityRef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string UniqueName { get; set; }
        public string Descriptor { get; set; }
    }
    public enum WorkItemRecentActivityType
    {
        Visited,
        Edited,
        Commented
    }
}
