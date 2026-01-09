using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADOApi.Data
{
    public class SecurityEventEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Organization { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Project { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Repository { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FindingId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string UserRole { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; }

        public Dictionary<string, object> Properties { get; set; } = new();

        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        [MaxLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PromptVersion { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PolicyVersion { get; set; } = string.Empty;

        // JSON storage for complex properties
        [Column(TypeName = "TEXT")]
        public string PropertiesJson { get; set; } = "{}";
    }

    public class RiskAcceptanceEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FindingId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Organization { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Project { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Repository { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Scope { get; set; } = "finding";

        [MaxLength(50)]
        public string Severity { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Justification { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string AcceptedBy { get; set; } = string.Empty;

        [Required]
        public DateTime AcceptedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string Fingerprint { get; set; } = string.Empty;

        public Dictionary<string, object> Metadata { get; set; } = new();

        // JSON storage for complex metadata
        [Column(TypeName = "TEXT")]
        public string MetadataJson { get; set; } = "{}";
    }

    public class PolicyOverrideEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FindingId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Organization { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Project { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Repository { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string OverrideType { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Justification { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string RequestedBy { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ApprovedBy { get; set; } = string.Empty;

        [Required]
        public DateTime RequestedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = false;

        public Dictionary<string, object> Metadata { get; set; } = new();

        // JSON storage for complex metadata
        [Column(TypeName = "TEXT")]
        public string MetadataJson { get; set; } = "{}";
    }

    public class AnalysisMetadataEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string AnalysisId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ModelProvider { get; set; } = string.Empty; // "AzureOpenAI" or "Ollama"

        [Required]
        [MaxLength(100)]
        public string ModelName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PromptVersion { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PolicyVersion { get; set; } = string.Empty;

        [Column(TypeName = "TEXT")]
        public string ConfidenceBreakdownJson { get; set; } = "{}";

        [Column(TypeName = "TEXT")]
        public string InputsUsedJson { get; set; } = "{}"; // SARIF, SBOM, Trivy flags, etc.

        [Required]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public Dictionary<string, object> ConfidenceBreakdown { get; set; } = new();
        public Dictionary<string, object> InputsUsed { get; set; } = new();
    }

    public class WorkItemProposalEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string RepoKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ChatMessageId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "TEXT")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string WorkItemType { get; set; } = string.Empty;

        [Required]
        public double Confidence { get; set; }

        [Required]
        [MaxLength(50)]
        public string Source { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Dictionary<string, object> Metadata { get; set; } = new();

        // JSON storage for complex metadata
        [Column(TypeName = "TEXT")]
        public string MetadataJson { get; set; } = "{}";
    }
}