using System;
using System.Collections.Generic;

namespace ADOApi.Models
{
    public class WorkItemFilterRequest
    {
        public string Project { get; set; } = string.Empty;
        public List<string>? WorkItemTypes { get; set; }
        public List<string>? States { get; set; }
        public List<string>? AssignedTo { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public DateTime? ChangedAfter { get; set; }
        public DateTime? ChangedBefore { get; set; }
        public List<string>? Tags { get; set; }
        public string? TitleContains { get; set; }
        public string? DescriptionContains { get; set; }
        public int? Priority { get; set; }
        public double? EffortHoursMin { get; set; }
        public double? EffortHoursMax { get; set; }
    }
} 