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
        public List<WorkItemRelation>? Relations { get; set; }

        [JsonProperty("_links")]
        public WorkItemLinks? Links { get; set; }

        [JsonProperty("url")]
        public string? Url { get; set; }
        public string? Title { get; internal set; }
        public string? IterationPath { get; internal set; }
        public List<WorkItemDetails> Parents { get; internal set; }
        public List<WorkItemDetails> Children { get; internal set; }
        public string? WorkItemTitle { get; internal set; }
        public string? ActivityDate { get; internal set; }
        public string? ActivityType { get; internal set; }
        public string? ProjectName { get; internal set; }
        public int WorkItemId { get; internal set; }
        public string? WorkItemType { get; internal set; }
        public string? WorkItemUrl { get; internal set; }
    }

    public class WorkItemRelation
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
