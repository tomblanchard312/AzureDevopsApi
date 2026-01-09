using System;

namespace ADOApi.Models
{
    public class PatMetadata
    {
        public required string DisplayName { get; set; }
        public required string Scope { get; set; }
        public DateTime ValidTo { get; set; }
        public bool AllOrgs { get; set; }
        public required string CreatedBy { get; set; }
        public string? RequesterIp { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
