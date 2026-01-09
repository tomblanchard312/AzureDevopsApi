using System.Threading.Tasks;
using System.Collections.Generic;
using ADOApi.Models;
using ADOApi.Data;

namespace ADOApi.Interfaces
{
    public interface ISecurityAdvisorService
    {
        Task<SecurityAnalysisResponse> AnalyzeSarifAsync(SarifAnalysisRequest request);
        Task<SecurityAnalysisResponse> AnalyzeSbomAsync(SbomAnalysisRequest request);
        Task<SecurityRecommendation> GenerateRecommendationAsync(string findingId, RecommendationRequest request);
        Task<DiffResponse> GenerateDiffAsync(string recommendationId, DiffRequest request);
        Task<ApplyResponse> ApplyRecommendationAsync(string recommendationId, ApplyRequest request);
        Task<List<SecurityFinding>> GetFindingsAsync(string? status = null);

        // Enterprise Governance Methods
        Task<PolicyOverrideResponse> RequestPolicyOverrideAsync(PolicyOverrideRequest request, string userId, string userRole);
        Task<PolicyOverrideResponse> ApprovePolicyOverrideAsync(string overrideId, string approvedBy);
        Task<List<PolicyOverride>> GetPolicyOverridesAsync(string? organization = null, string? project = null, bool activeOnly = true);
        Task<RiskAcceptanceResponse> AcceptRiskAsync(RiskAcceptanceRequest request, string acceptedBy);
        Task<List<RiskAcceptance>> GetRiskAcceptancesAsync(string? organization = null, string? project = null, bool activeOnly = true);
        Task<SecurityMetricsResponse> GetSecurityMetricsAsync(SecurityMetricsRequest request);
        Task<NoiseReductionPolicyResponse> CreateNoiseReductionPolicyAsync(NoiseReductionPolicyRequest request, string createdBy);
        Task<List<NoiseReductionPolicy>> GetNoiseReductionPoliciesAsync(string? organization = null, string? project = null, bool activeOnly = true);
        Task LogSecurityEventAsync(SecurityEvent securityEvent);
        Task<List<SecurityFinding>> GetFilteredFindingsAsync(string? organization = null, string? project = null, string? repository = null, string? status = null, string? severity = null);

        // Analysis Transparency Methods
        Task<AnalysisMetadata?> GetAnalysisMetadataAsync(string analysisId);
        Task<Dictionary<string, string>> GetCurrentVersionsAsync();
        Task<List<RiskAcceptanceEntity>> GetExpiringRiskAcceptancesAsync(DateTime expiringBefore);
    }
}