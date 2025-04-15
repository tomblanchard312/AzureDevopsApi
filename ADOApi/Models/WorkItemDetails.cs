namespace ADOApi.Models
{
    using System;
    using System.Collections.Generic;

    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

    using Newtonsoft.Json;

    public class WorkItemDetails
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("rev")]
        public int? Revision { get; set; }

        [JsonProperty("fields")]
        public Dictionary<string, object>? Fields { get; set; }

        [JsonProperty("relations")]
        public List<WorkItemApiRelation>? Relations { get; set; }

        [JsonProperty("_links")]
        public WorkItemLinks? Links { get; set; }

        [JsonProperty("url")]
        public string? Url { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? State { get; set; }
        public string? WorkItemType { get; set; }
        public string? AssignedTo { get; set; }
        public string? Project { get; set; }
        public string? AreaPath { get; set; }
        public string? IterationPath { get; set; }
        public double? Effort { get; set; }
        public double? RemainingWork { get; set; }
        public double? CompletedWork { get; set; }
        public string? Priority { get; set; }
        public string? Severity { get; set; }
        public string? Tags { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public string? Comments { get; set; }
        public int? ParentId { get; set; }
        public string? ParentTitle { get; set; }
        public string? ParentWorkItemType { get; set; }
        public string? ParentState { get; set; }
        public string? ParentAssignedTo { get; set; }
        public string? ParentProject { get; set; }
        public string? ParentAreaPath { get; set; }
        public string? ParentIterationPath { get; set; }
        public List<WorkItem> Parents { get; set; } = new();
        public List<WorkItem> Children { get; set; } = new();
        public string? WorkItemTitle { get; internal set; }
        public string? ActivityDate { get; internal set; }
        public string? ActivityType { get; internal set; }
        public string? ProjectName { get; internal set; }
        public int WorkItemId { get; internal set; }
        public string? WorkItemUrl { get; internal set; }
    }

    public class WorkItemApiRelation
    {
        [JsonProperty("rel")]
        public string? Rel { get; set; }

        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("attributes")]
        public Dictionary<string, object>? Attributes { get; set; }
    }

    public class WorkItemLinks
    {
        [JsonProperty("self")]
        public WorkItemLink? Self { get; set; }

        [JsonProperty("workItemUpdates")]
        public WorkItemLink? WorkItemUpdates { get; set; }

        [JsonProperty("workItemRevisions")]
        public WorkItemLink? WorkItemRevisions { get; set; }

        [JsonProperty("workItemHistory")]
        public WorkItemLink? WorkItemHistory { get; set; }

        [JsonProperty("html")]
        public WorkItemLink? Html { get; set; }

        [JsonProperty("workItemType")]
        public WorkItemLink? WorkItemType { get; set; }

        [JsonProperty("fields")]
        public WorkItemLink? Fields { get; set; }
    }

    public class WorkItemLink
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
    }
}
