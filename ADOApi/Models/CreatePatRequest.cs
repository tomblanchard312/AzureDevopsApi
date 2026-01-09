using System;

namespace ADOApi.Models
{
    public class CreatePatRequest
    {
        public required string DisplayName { get; set; }
        // Space-separated scopes
        public string? Scope { get; set; }
        // Optional requested expiry; if null, server will apply max lifetime
        public DateTime? ValidTo { get; set; }
    }
}
