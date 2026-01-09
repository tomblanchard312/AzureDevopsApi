using System;

namespace ADOApi.Models
{
    public class AuditEvent
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string? ActorObjectId { get; set; }
        public string? ActorUpn { get; set; }
        public string? ClientIp { get; set; }
        public string? UserAgent { get; set; }
        public string? CorrelationId { get; set; }
        public required string Action { get; set; }
        public string? TargetType { get; set; }
        public string? TargetId { get; set; }
        public string? Project { get; set; }
        public string? RepositoryId { get; set; }
        public int? WorkItemId { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
