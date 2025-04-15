using ADOApi.Services;
using System;

namespace ADOApi.Models
{
    public class RecentWorkItem
    {
        public int Id { get; init; }
        public required string Title { get; init; }
        public required string State { get; init; }
        public required string WorkItemType { get; init; }
        public required string AssignedTo { get; init; }
        public required DateTime ChangedDate { get; init; }
        public required string IdentityId { get; init; }
        public required DateTime ActivityDate { get; init; }
        public required string ActivityType { get; init; }
        public required string TeamProject { get; init; }
        public string? ProjectName { get; internal set; }
        public int WorkItemId { get; internal set; }
        public string? WorkItemTitle { get; internal set; }
        public string? WorkItemUrl { get; internal set; }
    }

    public class IdentityRef
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string DisplayName { get; set; }
        public required string UniqueName { get; set; }
        public required string Descriptor { get; set; }
    }
    public enum WorkItemRecentActivityType
    {
        Visited,
        Edited,
        Commented
    }
}
