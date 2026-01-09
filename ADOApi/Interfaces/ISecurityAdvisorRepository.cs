using System.Threading.Tasks;
using System.Collections.Generic;
using ADOApi.Data;

namespace ADOApi.Interfaces
{
    public interface ISecurityAdvisorRepository
    {
        // Security Events
        Task AddSecurityEventAsync(SecurityEventEntity securityEvent);
        Task<IEnumerable<SecurityEventEntity>> GetSecurityEventsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? organization = null,
            string? project = null,
            string? eventType = null);

        // Risk Acceptances
        Task AddRiskAcceptanceAsync(RiskAcceptanceEntity riskAcceptance);
        Task<RiskAcceptanceEntity?> GetRiskAcceptanceByIdAsync(int id);
        Task UpdateRiskAcceptanceAsync(RiskAcceptanceEntity riskAcceptance);
        Task<IEnumerable<RiskAcceptanceEntity>> GetRiskAcceptancesAsync(
            string? organization = null,
            string? project = null,
            bool activeOnly = true);

        // Policy Overrides
        Task AddPolicyOverrideAsync(PolicyOverrideEntity policyOverride);
        Task<PolicyOverrideEntity?> GetPolicyOverrideByIdAsync(int id);
        Task UpdatePolicyOverrideAsync(PolicyOverrideEntity policyOverride);
        Task<IEnumerable<PolicyOverrideEntity>> GetPolicyOverridesAsync(
            string? organization = null,
            string? project = null,
            bool activeOnly = true);

        // Analysis Metadata
        Task AddAnalysisMetadataAsync(AnalysisMetadataEntity analysisMetadata);
        Task<AnalysisMetadataEntity?> GetAnalysisMetadataAsync(string analysisId);
        Task<IEnumerable<AnalysisMetadataEntity>> GetAnalysisMetadataByDateRangeAsync(
            DateTime startDate,
            DateTime endDate);

        // Version information
        Task<Dictionary<string, string>> GetCurrentVersionsAsync();

        // Security metrics
        Task<SecurityMetricsData> GetSecurityMetricsAsync(DateTime startDate, DateTime endDate, string? organization = null, string? project = null);

        // Save changes
        Task SaveChangesAsync();
    }

    public class SecurityMetricsData
    {
        public int TotalFindings { get; set; }
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
}