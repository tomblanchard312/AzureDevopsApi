using System.Text.Json.Serialization;

namespace ADOApi.Models
{
    public class SarifAnalysisRequest
    {
        public required string SarifContent { get; set; }
        public string? Repository { get; set; }
        public string? Branch { get; set; }
    }

    public class SbomAnalysisRequest
    {
        public required string SbomContent { get; set; }
        public string? Format { get; set; } // "cyclonedx", "spdx"
    }

    public class SecurityAnalysisResponse
    {
        public List<SecurityFinding> Findings { get; set; } = new();
        public int TotalFindings { get; set; }
        public int HighSeverityCount { get; set; }
        public int CriticalSeverityCount { get; set; }
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString();
    }

    public class SecurityFinding
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Unknown"; // Critical, High, Medium, Low, Info
        public string Category { get; set; } = string.Empty; // SAST, DAST, SCA, etc.
        public string FilePath { get; set; } = string.Empty;
        public int? LineNumber { get; set; }
        public string RuleId { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string Status { get; set; } = "Open"; // Open, Investigating, Fixed, Accepted, FalsePositive
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? AssignedTo { get; set; }
        public List<SecurityRecommendation> Recommendations { get; set; } = new();
    }

    public class RecommendationRequest
    {
        public string? Context { get; set; }
        public string? CodeSnippet { get; set; }
        public string? Language { get; set; }
    }

    public class SecurityRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FindingId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RiskAssessment { get; set; } = string.Empty;
        public string RemediationSteps { get; set; } = string.Empty;
        public string CodeChanges { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public string Confidence { get; set; } = "Medium"; // High, Medium, Low
        public double ConfidenceScore { get; set; } = 0.5; // Numeric confidence 0-1
        public string ConfidenceExplanation { get; set; } = string.Empty; // Detailed explanation of confidence calculation
        public List<string> WhyNotFixReasons { get; set; } = new(); // Reasons why this shouldn't be auto-fixed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Approved { get; set; } = false;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class ConfidenceScoringDetails
    {
        public double SeverityScore { get; set; } // 0-1 based on finding severity
        public double FixPatternScore { get; set; } // 0-1 based on known fix patterns
        public double ChangeRiskScore { get; set; } // 0-1 based on change risk assessment
        public double ModelAgreementScore { get; set; } // 0-1 based on model confidence
        public double OverallScore { get; set; } // Final weighted score
        public string Explanation { get; set; } = string.Empty;
        public List<string> ContributingFactors { get; set; } = new();
    }

    public class DiffRequest
    {
        public string? FilePath { get; set; }
        public string? OriginalContent { get; set; }
        public string? ModifiedContent { get; set; }
    }

    public class DiffResponse
    {
        public string DiffContent { get; set; } = string.Empty;
        public List<DiffHunk> Hunks { get; set; } = new();
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    public class DiffHunk
    {
        public int OldStart { get; set; }
        public int OldLines { get; set; }
        public int NewStart { get; set; }
        public int NewLines { get; set; }
        public List<string> Lines { get; set; } = new();
    }

    public class ApplyRequest
    {
        public string? Repository { get; set; }
        public string? Branch { get; set; }
        public string? CommitMessage { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
    }

    public class ApplyResponse
    {
        public bool Success { get; set; }
        public string? CommitId { get; set; }
        public string? PullRequestUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PullRequestCommentRequest
    {
        public required string Organization { get; set; }
        public required string Project { get; set; }
        public required int PullRequestId { get; set; }
        public required string RepositoryId { get; set; }
        public List<string>? FindingIds { get; set; } // Optional: specific findings to include
        public bool PreviewOnly { get; set; } = false; // If true, return comment without posting
    }

    public class PullRequestCommentResponse
    {
        public bool Success { get; set; }
        public string? CommentMarkdown { get; set; }
        public int? ThreadId { get; set; }
        public string? CommentUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? PostedAt { get; set; }
    }

    public class SecurityReviewComment
    {
        public string Organization { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public int PullRequestId { get; set; }
        public string RepositoryId { get; set; } = string.Empty;
        public List<SecurityFindingComment> Findings { get; set; } = new();
        public string OverallAssessment { get; set; } = string.Empty;
        public string ApprovalStatus { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class SecurityFindingComment
    {
        public string FindingId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int? LineNumber { get; set; }
        public string? DiffSnippet { get; set; }
        public List<string> WhyNotFixReasons { get; set; } = new();
        public string Recommendation { get; set; } = string.Empty;
    }

    // Inline comment models
    public class InlineCommentRequest
    {
        public required string Organization { get; set; }
        public required string Project { get; set; }
        public required int PullRequestId { get; set; }
        public required string RepositoryId { get; set; }
        public required string FindingId { get; set; }
        public bool PreviewOnly { get; set; } = false;
    }

    public class InlineCommentResponse
    {
        public bool Success { get; set; }
        public string? CommentMarkdown { get; set; }
        public int? ThreadId { get; set; }
        public string? CommentUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? PostedAt { get; set; }
    }

    // Thread resolution models
    public class ThreadResolutionRequest
    {
        public required string Organization { get; set; }
        public required string Project { get; set; }
        public required int PullRequestId { get; set; }
        public required string RepositoryId { get; set; }
        public List<int>? ThreadIds { get; set; } // Optional: specific threads to check
    }

    public class ThreadResolutionResponse
    {
        public bool Success { get; set; }
        public List<ResolvedThread> ResolvedThreads { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class ResolvedThread
    {
        public int ThreadId { get; set; }
        public string FindingId { get; set; } = string.Empty;
        public string ResolutionComment { get; set; } = string.Empty;
        public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;
    }

    // PR status models
    public class PrStatusRequest
    {
        public required string Organization { get; set; }
        public required string Project { get; set; }
        public required int PullRequestId { get; set; }
        public required string RepositoryId { get; set; }
        public string? TargetUrl { get; set; } // Optional link to Security Advisor UI
    }

    public class PrStatusResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty; // "succeeded", "failed", "pending"
        public string Description { get; set; } = string.Empty;
        public string? StatusUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Enterprise Governance Models

    public class PolicyOverride
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FindingId { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string OverrideType { get; set; } = string.Empty; // "severity", "suppress", "accept"
        public string Justification { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty; // Entra ID UPN
        public string ApprovedBy { get; set; } = string.Empty; // Must be ADO.Admin
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; } // Optional expiration
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class RiskAcceptance
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FindingId { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string Scope { get; set; } = "finding"; // "finding", "rule", "repository", "project"
        public string Severity { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public string AcceptedBy { get; set; } = string.Empty; // Entra ID UPN
        public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; } // Optional expiration
        public bool IsActive { get; set; } = true;
        public string Fingerprint { get; set; } = string.Empty; // For noise reduction
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SecurityEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventType { get; set; } = string.Empty; // "finding_created", "recommendation_generated", "override_requested", etc.
        public string Organization { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string FindingId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty; // Entra ID UPN
        public string UserRole { get; set; } = string.Empty; // "ReadOnly", "Contributor", "Admin"
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Properties { get; set; } = new();
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;
        public string PolicyVersion { get; set; } = string.Empty;
    }

    public class SecurityMetrics
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string Organization { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public Dictionary<string, int> FindingsBySeverity { get; set; } = new();
        public Dictionary<string, int> FindingsByStatus { get; set; } = new();
        public Dictionary<string, int> FindingsByCategory { get; set; } = new();
        public int TotalOverrides { get; set; }
        public int TotalRiskAcceptances { get; set; }
        public int TotalRecommendations { get; set; }
        public int TotalAppliedFixes { get; set; }
        public double AverageResolutionTimeHours { get; set; }
        public Dictionary<string, int> EventsByType { get; set; } = new();
    }

    public class NoiseReductionPolicy
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Organization { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public string Fingerprint { get; set; } = string.Empty;
        public string Action { get; set; } = "suppress"; // "suppress", "reduce_severity", "ignore"
        public string Reason { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> Conditions { get; set; } = new();
    }

    // Request/Response models for governance endpoints

    public class PolicyOverrideRequest
    {
        public required string FindingId { get; set; }
        public required string OverrideType { get; set; }
        public required string Justification { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class PolicyOverrideResponse
    {
        public bool Success { get; set; }
        public PolicyOverride? Override { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RiskAcceptanceRequest
    {
        public required string FindingId { get; set; }
        public required string Scope { get; set; }
        public required string Justification { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class RiskAcceptanceResponse
    {
        public bool Success { get; set; }
        public RiskAcceptance? Acceptance { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class SecurityMetricsRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Organization { get; set; }
        public string? Project { get; set; }
        public string? Repository { get; set; }
    }

    public class SecurityMetricsResponse
    {
        public SecurityMetrics? Metrics { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class NoiseReductionPolicyRequest
    {
        public required string RuleId { get; set; }
        public required string Fingerprint { get; set; }
        public required string Action { get; set; }
        public required string Reason { get; set; }
        public Dictionary<string, object>? Conditions { get; set; }
    }

    public class NoiseReductionPolicyResponse
    {
        public bool Success { get; set; }
        public NoiseReductionPolicy? Policy { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Analysis Metadata DTOs
    public class AnalysisMetadata
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AnalysisId { get; set; } = string.Empty;
        public string ModelProvider { get; set; } = string.Empty; // "AzureOpenAI" or "Ollama"
        public string ModelName { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;
        public string PolicyVersion { get; set; } = string.Empty;
        public Dictionary<string, object> ConfidenceBreakdown { get; set; } = new();
        public Dictionary<string, object> InputsUsed { get; set; } = new(); // SARIF, SBOM, Trivy flags, etc.
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    public class AnalysisMetadataResponse
    {
        public AnalysisMetadata? Metadata { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Version information DTOs
    public class VersionInfo
    {
        public string PromptVersion { get; set; } = "v1.0";
        public string PolicyVersion { get; set; } = "v1.0";
    }

    public class VersionInfoResponse
    {
        public VersionInfo? Versions { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Re-analysis DTOs
    public class ReAnalysisRequest
    {
        public string PrId { get; set; } = string.Empty;
        public string? CommitSha { get; set; }
        public string? Repository { get; set; }
        public string? Project { get; set; }
        public string? Organization { get; set; }
    }

    public class ReAnalysisResponse
    {
        public bool Success { get; set; }
        public string? AnalysisId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? StartedAt { get; set; }
    }

    // Expiring risk acceptances DTOs
    public class ExpiringRiskAcceptance
    {
        public string Id { get; set; } = string.Empty;
        public string FindingId { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string AcceptedBy { get; set; } = string.Empty;
        public DateTime AcceptedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int DaysUntilExpiry { get; set; }
    }

    public class ExpiringRiskAcceptancesResponse
    {
        public List<ExpiringRiskAcceptance> ExpiringAcceptances { get; set; } = new();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}