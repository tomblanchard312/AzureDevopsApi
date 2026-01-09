using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADOApi.Data
{
    public class RepoSnapshot
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(500)]
        public string RepoKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Branch { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CommitSha { get; set; } = string.Empty;

        [Required]
        public DateTime CapturedUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? Summary { get; set; }

        [Column(TypeName = "TEXT")]
        public string? StatsJson { get; set; }
    }

    public class RepositoryMemory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(500)]
        public string RepoKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string MemoryType { get; set; } = string.Empty; // Architecture | Security | Convention | Constraint | Domain

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "TEXT")]
        public string Content { get; set; } = string.Empty; // markdown

        [Required]
        [MaxLength(50)]
        public string Source { get; set; } = string.Empty; // Human | CodeScan | Agent

        [Required]
        public double Confidence { get; set; } // 0-1

        [Required]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastValidatedUtc { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }

    public class CodeInsight
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(500)]
        public string RepoKey { get; set; } = string.Empty;

        public Guid? SnapshotId { get; set; }

        [Required]
        [MaxLength(50)]
        public string InsightType { get; set; } = string.Empty; // BugPattern | Vulnerability | Smell | Debt | Improvement | DocsGap

        [MaxLength(200)]
        public string? RuleId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "TEXT")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = string.Empty; // Low | Medium | High | Critical

        [Required]
        public double Confidence { get; set; } // 0-1

        [MaxLength(1000)]
        public string? FilePath { get; set; }

        public int? StartLine { get; set; }
        public int? EndLine { get; set; }

        [Required]
        [MaxLength(500)]
        public string Fingerprint { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Open"; // Open | Fixed | Accepted | Suppressed | Deferred | Duplicate

        [Required]
        public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "TEXT")]
        public string? TagsJson { get; set; }

        [Column(TypeName = "TEXT")]
        public string? EvidenceJson { get; set; }

        [Column(TypeName = "TEXT")]
        public string? RelatedMemoryIdsJson { get; set; }

        // Navigation property
        [ForeignKey("SnapshotId")]
        public virtual RepoSnapshot? Snapshot { get; set; }
    }

    public class WorkItemLink
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(500)]
        public string RepoKey { get; set; } = string.Empty;

        [Required]
        public Guid InsightId { get; set; }

        [Required]
        public int WorkItemId { get; set; }

        [Required]
        [MaxLength(50)]
        public string WorkItemType { get; set; } = string.Empty; // Bug | Task | Feature | Epic

        [MaxLength(50)]
        public string? State { get; set; }

        [Required]
        [MaxLength(20)]
        public string Disposition { get; set; } = string.Empty; // Proposed | Created | Rejected | Deferred | Accepted | Closed

        [MaxLength(500)]
        public string? Reason { get; set; }

        [Required]
        [MaxLength(200)]
        public string CreatedBy { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ClosedUtc { get; set; }

        // Navigation property
        [ForeignKey("InsightId")]
        public virtual CodeInsight? Insight { get; set; }
    }

    public class AgentRun
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(500)]
        public string RepoKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string RunType { get; set; } = string.Empty; // Ingest | InsightRefresh | WorkItemPropose | WorkItemCreate

        [Required]
        [MaxLength(50)]
        public string ModelProvider { get; set; } = string.Empty; // AzureOpenAI | Ollama

        [Required]
        [MaxLength(100)]
        public string ModelName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PromptVersion { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PolicyVersion { get; set; } = string.Empty;

        [Required]
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedUtc { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Started"; // Started | Succeeded | Failed | Canceled

        [Column(TypeName = "TEXT")]
        public string? InputSummaryJson { get; set; }

        [Column(TypeName = "TEXT")]
        public string? OutputSummaryJson { get; set; }

        [Column(TypeName = "TEXT")]
        public string? Error { get; set; }

        [MaxLength(100)]
        public string? CorrelationId { get; set; }

        // Navigation properties
        public virtual ICollection<AgentDecision> Decisions { get; set; } = new List<AgentDecision>();
    }

    public class AgentDecision
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(500)]
        public string RepoKey { get; set; } = string.Empty;

        [Required]
        public Guid RunId { get; set; }

        [Required]
        [MaxLength(50)]
        public string DecisionType { get; set; } = string.Empty; // CreateWorkItem | Suppress | AcceptRisk | RecommendFeature | RecommendDoc

        [Required]
        [MaxLength(50)]
        public string TargetType { get; set; } = string.Empty; // Insight | Memory | Repo

        [Required]
        [MaxLength(200)]
        public string TargetId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Decision { get; set; } = string.Empty; // Allow | Deny | Defer

        [Required]
        [Column(TypeName = "TEXT")]
        public string Justification { get; set; } = string.Empty;

        [Required]
        public double Confidence { get; set; }

        [Required]
        [MaxLength(50)]
        public string CreatedBy { get; set; } = string.Empty; // Agent | Human

        [Required]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("RunId")]
        public virtual AgentRun? Run { get; set; }
    }
}