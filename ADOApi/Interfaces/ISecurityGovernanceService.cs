using System.Threading.Tasks;
using System.Collections.Generic;
using ADOApi.Models;

namespace ADOApi.Interfaces
{
    public interface ISecurityGovernanceService
    {
        Task<PolicyOverrideResponse> RequestPolicyOverrideAsync(PolicyOverrideRequest request, string userId, string userRole);
        Task<PolicyOverrideResponse> ApprovePolicyOverrideAsync(string overrideId, string approvedBy);
        Task<List<PolicyOverride>> GetPolicyOverridesAsync(string? organization = null, string? project = null, bool activeOnly = true);
        Task<RiskAcceptanceResponse> AcceptRiskAsync(RiskAcceptanceRequest request, string acceptedBy);
        Task<List<RiskAcceptance>> GetRiskAcceptancesAsync(string? organization = null, string? project = null, bool activeOnly = true);
        Task<SecurityMetricsResponse> GetSecurityMetricsAsync(SecurityMetricsRequest request);
        Task<NoiseReductionPolicyResponse> CreateNoiseReductionPolicyAsync(NoiseReductionPolicyRequest request, string createdBy);
        Task<List<NoiseReductionPolicy>> GetNoiseReductionPoliciesAsync(string? organization = null, string? project = null, bool activeOnly = true);
        Task LogSecurityEventAsync(SecurityEvent securityEvent);
    }
}