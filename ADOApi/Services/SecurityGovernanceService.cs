using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Models;
using ADOApi.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ADOApi.Services
{
    public class SecurityGovernanceService : ISecurityGovernanceService
    {
        private readonly ILogger<SecurityGovernanceService> _logger;
        private readonly IAuditLogger _auditLogger;
        private readonly ISecurityAdvisorRepository _repository;
        private readonly Dictionary<string, NoiseReductionPolicy> _noisePolicies = new();

        public SecurityGovernanceService(
            ILogger<SecurityGovernanceService> logger,
            IAuditLogger auditLogger,
            ISecurityAdvisorRepository repository)
        {
            _logger = logger;
            _auditLogger = auditLogger;
            _repository = repository;
        }

        public async Task<PolicyOverrideResponse> RequestPolicyOverrideAsync(PolicyOverrideRequest request, string userId, string userRole)
        {
            // Validate user role - only Contributors and Admins can request overrides
            if (userRole != "Contributor" && userRole != "Admin")
            {
                return new PolicyOverrideResponse
                {
                    Success = false,
                    ErrorMessage = "Insufficient permissions to request policy override"
                };
            }

            var policyOverrideEntity = new PolicyOverrideEntity
            {
                FindingId = request.FindingId,
                OverrideType = request.OverrideType,
                Justification = request.Justification,
                RequestedBy = userId,
                RequestedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                Metadata = request.Metadata ?? new Dictionary<string, object>(),
                IsActive = false // Requires admin approval
            };

            await _repository.AddPolicyOverrideAsync(policyOverrideEntity);
            await _repository.SaveChangesAsync();

            // Log security event
            await LogSecurityEventAsync(new SecurityEvent
            {
                EventType = "policy_override_requested",
                FindingId = request.FindingId,
                UserId = userId,
                UserRole = userRole,
                Properties = new Dictionary<string, object> { ["overrideType"] = request.OverrideType }
            });

            _logger.LogInformation("Policy override requested for finding {FindingId} by user {UserId}",
                request.FindingId, userId);

            // Convert to DTO for response
            var policyOverride = new PolicyOverride
            {
                Id = policyOverrideEntity.Id.ToString(),
                FindingId = policyOverrideEntity.FindingId,
                OverrideType = policyOverrideEntity.OverrideType,
                Justification = policyOverrideEntity.Justification,
                RequestedBy = policyOverrideEntity.RequestedBy,
                RequestedAt = policyOverrideEntity.RequestedAt,
                ExpiresAt = policyOverrideEntity.ExpiresAt,
                IsActive = policyOverrideEntity.IsActive,
                Metadata = policyOverrideEntity.Metadata
            };

            return new PolicyOverrideResponse
            {
                Success = true,
                Override = policyOverride
            };
        }

        public async Task<PolicyOverrideResponse> ApprovePolicyOverrideAsync(string overrideId, string approvedBy)
        {
            if (!int.TryParse(overrideId, out var id))
            {
                return new PolicyOverrideResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid override ID"
                };
            }

            var policyOverrideEntity = await _repository.GetPolicyOverrideByIdAsync(id);
            if (policyOverrideEntity == null)
            {
                return new PolicyOverrideResponse
                {
                    Success = false,
                    ErrorMessage = "Policy override not found"
                };
            }

            // Only admins can approve overrides
            // This would be validated by the controller using ADO.Admin policy

            policyOverrideEntity.ApprovedBy = approvedBy;
            policyOverrideEntity.ApprovedAt = DateTime.UtcNow;
            policyOverrideEntity.IsActive = true;

            await _repository.UpdatePolicyOverrideAsync(policyOverrideEntity);
            await _repository.SaveChangesAsync();

            // Log security event
            await LogSecurityEventAsync(new SecurityEvent
            {
                EventType = "policy_override_approved",
                FindingId = policyOverrideEntity.FindingId,
                UserId = approvedBy,
                UserRole = "Admin",
                Properties = new Dictionary<string, object> { ["overrideId"] = overrideId }
            });

            _logger.LogInformation("Policy override {OverrideId} approved by {ApprovedBy}", overrideId, approvedBy);

            // Convert to DTO for response
            var policyOverride = new PolicyOverride
            {
                Id = policyOverrideEntity.Id.ToString(),
                FindingId = policyOverrideEntity.FindingId,
                OverrideType = policyOverrideEntity.OverrideType,
                Justification = policyOverrideEntity.Justification,
                RequestedBy = policyOverrideEntity.RequestedBy,
                ApprovedBy = policyOverrideEntity.ApprovedBy,
                RequestedAt = policyOverrideEntity.RequestedAt,
                ApprovedAt = policyOverrideEntity.ApprovedAt ?? DateTime.MinValue,
                ExpiresAt = policyOverrideEntity.ExpiresAt ?? DateTime.MinValue,
                IsActive = policyOverrideEntity.IsActive,
                Metadata = policyOverrideEntity.Metadata
            };

            return new PolicyOverrideResponse
            {
                Success = true,
                Override = policyOverride
            };
        }

        public async Task<List<PolicyOverride>> GetPolicyOverridesAsync(string? organization = null, string? project = null, bool activeOnly = true)
        {
            var entities = await _repository.GetPolicyOverridesAsync(organization, project, activeOnly);

            return entities.Select(e => new PolicyOverride
            {
                Id = e.Id.ToString(),
                FindingId = e.FindingId,
                Organization = e.Organization,
                Project = e.Project,
                Repository = e.Repository,
                OverrideType = e.OverrideType,
                Justification = e.Justification,
                RequestedBy = e.RequestedBy,
                ApprovedBy = e.ApprovedBy,
                RequestedAt = e.RequestedAt,
                ApprovedAt = e.ApprovedAt ?? DateTime.MinValue,
                ExpiresAt = e.ExpiresAt ?? DateTime.MinValue,
                IsActive = e.IsActive,
                Metadata = e.Metadata
            }).ToList();
        }

        public async Task<RiskAcceptanceResponse> AcceptRiskAsync(RiskAcceptanceRequest request, string acceptedBy)
        {
            var riskAcceptanceEntity = new RiskAcceptanceEntity
            {
                FindingId = request.FindingId,
                Scope = request.Scope,
                Justification = request.Justification,
                AcceptedBy = acceptedBy,
                AcceptedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                Metadata = request.Metadata ?? new Dictionary<string, object>(),
                IsActive = true
            };

            await _repository.AddRiskAcceptanceAsync(riskAcceptanceEntity);
            await _repository.SaveChangesAsync();

            // Log security event
            await LogSecurityEventAsync(new SecurityEvent
            {
                EventType = "risk_accepted",
                FindingId = request.FindingId,
                UserId = acceptedBy,
                UserRole = "Contributor",
                Properties = new Dictionary<string, object> { ["scope"] = request.Scope }
            });

            _logger.LogInformation("Risk accepted for finding {FindingId} by user {AcceptedBy}",
                request.FindingId, acceptedBy);

            // Convert to DTO for response
            var riskAcceptance = new RiskAcceptance
            {
                Id = riskAcceptanceEntity.Id.ToString(),
                FindingId = riskAcceptanceEntity.FindingId,
                Scope = riskAcceptanceEntity.Scope,
                Justification = riskAcceptanceEntity.Justification,
                AcceptedBy = riskAcceptanceEntity.AcceptedBy,
                AcceptedAt = riskAcceptanceEntity.AcceptedAt,
                ExpiresAt = riskAcceptanceEntity.ExpiresAt,
                IsActive = riskAcceptanceEntity.IsActive,
                Fingerprint = riskAcceptanceEntity.Fingerprint,
                Metadata = riskAcceptanceEntity.Metadata
            };

            return new RiskAcceptanceResponse
            {
                Success = true,
                Acceptance = riskAcceptance
            };
        }

        public async Task<List<RiskAcceptance>> GetRiskAcceptancesAsync(string? organization = null, string? project = null, bool activeOnly = true)
        {
            var entities = await _repository.GetRiskAcceptancesAsync(organization, project, activeOnly);

            return entities.Select(e => new RiskAcceptance
            {
                Id = e.Id.ToString(),
                FindingId = e.FindingId,
                Organization = e.Organization,
                Project = e.Project,
                Repository = e.Repository,
                Scope = e.Scope,
                Severity = e.Severity,
                Justification = e.Justification,
                AcceptedBy = e.AcceptedBy,
                AcceptedAt = e.AcceptedAt,
                ExpiresAt = e.ExpiresAt,
                IsActive = e.IsActive,
                Fingerprint = e.Fingerprint,
                Metadata = e.Metadata
            }).ToList();
        }

        public async Task<SecurityMetricsResponse> GetSecurityMetricsAsync(SecurityMetricsRequest request)
        {
            var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = request.EndDate ?? DateTime.UtcNow;

            var metricsData = await _repository.GetSecurityMetricsAsync(startDate, endDate, request.Organization, request.Project);

            var metrics = new SecurityMetrics
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                Organization = request.Organization ?? "",
                Project = request.Project ?? "",
                FindingsBySeverity = metricsData.FindingsBySeverity,
                FindingsByStatus = metricsData.FindingsByStatus,
                FindingsByCategory = metricsData.FindingsByCategory,
                TotalOverrides = metricsData.TotalOverrides,
                TotalRiskAcceptances = metricsData.TotalRiskAcceptances,
                TotalRecommendations = metricsData.TotalRecommendations,
                TotalAppliedFixes = metricsData.TotalAppliedFixes,
                AverageResolutionTimeHours = metricsData.AverageResolutionTimeHours,
                EventsByType = metricsData.EventsByType
            };

            return new SecurityMetricsResponse
            {
                Metrics = metrics
            };
        }

        public async Task<NoiseReductionPolicyResponse> CreateNoiseReductionPolicyAsync(NoiseReductionPolicyRequest request, string createdBy)
        {
            return await Task.Run(() =>
            {
                var policy = new NoiseReductionPolicy
                {
                    RuleId = request.RuleId,
                    Fingerprint = request.Fingerprint,
                    Action = request.Action,
                    Reason = request.Reason,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    Conditions = request.Conditions ?? new Dictionary<string, object>(),
                    IsActive = true
                };

                _noisePolicies[policy.Id] = policy;

                // Log security event
                LogSecurityEvent("noise_policy_created", "", "", "", "", createdBy, "Admin",
                    new Dictionary<string, object> { ["ruleId"] = request.RuleId, ["action"] = request.Action });

                _logger.LogInformation("Noise reduction policy created for rule {RuleId} by {CreatedBy}",
                    request.RuleId, createdBy);

                return new NoiseReductionPolicyResponse
                {
                    Success = true,
                    Policy = policy
                };
            });
        }

        public async Task<List<NoiseReductionPolicy>> GetNoiseReductionPoliciesAsync(string? organization = null, string? project = null, bool activeOnly = true)
        {
            return await Task.Run(() =>
            {
                var policies = _noisePolicies.Values.AsQueryable();

                if (!string.IsNullOrEmpty(organization))
                    policies = policies.Where(p => p.Organization == organization);

                if (!string.IsNullOrEmpty(project))
                    policies = policies.Where(p => p.Project == project);

                if (activeOnly)
                    policies = policies.Where(p => p.IsActive);

                return policies.OrderByDescending(p => p.CreatedAt).ToList();
            });
        }

        public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
        {
            var securityEventEntity = new SecurityEventEntity
            {
                EventType = securityEvent.EventType,
                Organization = securityEvent.Organization,
                Project = securityEvent.Project,
                Repository = securityEvent.Repository,
                FindingId = securityEvent.FindingId,
                UserId = securityEvent.UserId,
                UserRole = securityEvent.UserRole,
                Timestamp = securityEvent.Timestamp,
                Properties = securityEvent.Properties,
                IpAddress = securityEvent.IpAddress,
                UserAgent = securityEvent.UserAgent
            };

            await _repository.AddSecurityEventAsync(securityEventEntity);
            await _repository.SaveChangesAsync();

            // In production, this would be persisted to a database or audit log
            _logger.LogInformation("Security event logged: {EventType} by {UserId} for finding {FindingId}",
                securityEvent.EventType, securityEvent.UserId, securityEvent.FindingId);
        }

        private void LogSecurityEvent(string eventType, string organization, string project, string repository,
            string findingId, string userId, string userRole, Dictionary<string, object> properties)
        {
            var securityEvent = new SecurityEvent
            {
                EventType = eventType,
                Organization = organization,
                Project = project,
                Repository = repository,
                FindingId = findingId,
                UserId = userId,
                UserRole = userRole,
                Properties = properties
            };

            // Use the async version but don't await in this context
            _ = LogSecurityEventAsync(securityEvent);
        }
    }
}