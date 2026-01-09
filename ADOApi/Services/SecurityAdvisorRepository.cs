using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ADOApi.Services
{
    public class SecurityAdvisorRepository : ISecurityAdvisorRepository
    {
        private readonly SecurityAdvisorDbContext _context;
        private readonly ILogger<SecurityAdvisorRepository> _logger;

        public SecurityAdvisorRepository(
            SecurityAdvisorDbContext context,
            ILogger<SecurityAdvisorRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddSecurityEventAsync(SecurityEventEntity securityEvent)
        {
            // Serialize properties to JSON
            securityEvent.PropertiesJson = System.Text.Json.JsonSerializer.Serialize(securityEvent.Properties);

            await _context.SecurityEvents.AddAsync(securityEvent);
            _logger.LogInformation("Added security event: {EventType} for user {UserId}", securityEvent.EventType, securityEvent.UserId);
        }

        public async Task<IEnumerable<SecurityEventEntity>> GetSecurityEventsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? organization = null,
            string? project = null,
            string? eventType = null)
        {
            var query = _context.SecurityEvents.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(organization))
                query = query.Where(e => e.Organization == organization);

            if (!string.IsNullOrEmpty(project))
                query = query.Where(e => e.Project == project);

            if (!string.IsNullOrEmpty(eventType))
                query = query.Where(e => e.EventType == eventType);

            var events = await query
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();

            // Deserialize properties from JSON
            foreach (var securityEvent in events)
            {
                try
                {
                    securityEvent.Properties = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(securityEvent.PropertiesJson) ?? new();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize properties for security event {Id}", securityEvent.Id);
                    securityEvent.Properties = new();
                }
            }

            return events;
        }

        public async Task AddRiskAcceptanceAsync(RiskAcceptanceEntity riskAcceptance)
        {
            // Serialize metadata to JSON
            riskAcceptance.MetadataJson = System.Text.Json.JsonSerializer.Serialize(riskAcceptance.Metadata);

            await _context.RiskAcceptances.AddAsync(riskAcceptance);
            _logger.LogInformation("Added risk acceptance for finding {FindingId} by {AcceptedBy}", riskAcceptance.FindingId, riskAcceptance.AcceptedBy);
        }

        public async Task<RiskAcceptanceEntity?> GetRiskAcceptanceByIdAsync(int id)
        {
            var acceptance = await _context.RiskAcceptances.FindAsync(id);
            if (acceptance != null)
            {
                try
                {
                    acceptance.Metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(acceptance.MetadataJson) ?? new();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize metadata for risk acceptance {Id}", id);
                    acceptance.Metadata = new();
                }
            }
            return acceptance;
        }

        public async Task UpdateRiskAcceptanceAsync(RiskAcceptanceEntity riskAcceptance)
        {
            // Serialize metadata to JSON
            riskAcceptance.MetadataJson = System.Text.Json.JsonSerializer.Serialize(riskAcceptance.Metadata);

            _context.RiskAcceptances.Update(riskAcceptance);
            _logger.LogInformation("Updated risk acceptance {Id}", riskAcceptance.Id);
        }

        public async Task<IEnumerable<RiskAcceptanceEntity>> GetRiskAcceptancesAsync(
            string? organization = null,
            string? project = null,
            bool activeOnly = true)
        {
            var query = _context.RiskAcceptances.AsQueryable();

            if (!string.IsNullOrEmpty(organization))
                query = query.Where(a => a.Organization == organization);

            if (!string.IsNullOrEmpty(project))
                query = query.Where(a => a.Project == project);

            if (activeOnly)
                query = query.Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow));

            var acceptances = await query
                .OrderByDescending(a => a.AcceptedAt)
                .ToListAsync();

            // Deserialize metadata from JSON
            foreach (var acceptance in acceptances)
            {
                try
                {
                    acceptance.Metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(acceptance.MetadataJson) ?? new();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize metadata for risk acceptance {Id}", acceptance.Id);
                    acceptance.Metadata = new();
                }
            }

            return acceptances;
        }

        public async Task AddPolicyOverrideAsync(PolicyOverrideEntity policyOverride)
        {
            // Serialize metadata to JSON
            policyOverride.MetadataJson = System.Text.Json.JsonSerializer.Serialize(policyOverride.Metadata);

            await _context.PolicyOverrides.AddAsync(policyOverride);
            _logger.LogInformation("Added policy override request for finding {FindingId} by {RequestedBy}", policyOverride.FindingId, policyOverride.RequestedBy);
        }

        public async Task<PolicyOverrideEntity?> GetPolicyOverrideByIdAsync(int id)
        {
            var policyOverride = await _context.PolicyOverrides.FindAsync(id);
            if (policyOverride != null)
            {
                try
                {
                    policyOverride.Metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(policyOverride.MetadataJson) ?? new();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize metadata for policy override {Id}", id);
                    policyOverride.Metadata = new();
                }
            }
            return policyOverride;
        }

        public async Task UpdatePolicyOverrideAsync(PolicyOverrideEntity policyOverride)
        {
            // Serialize metadata to JSON
            policyOverride.MetadataJson = System.Text.Json.JsonSerializer.Serialize(policyOverride.Metadata);

            _context.PolicyOverrides.Update(policyOverride);
            _logger.LogInformation("Updated policy override {Id}", policyOverride.Id);
        }

        public async Task<IEnumerable<PolicyOverrideEntity>> GetPolicyOverridesAsync(
            string? organization = null,
            string? project = null,
            bool activeOnly = true)
        {
            var query = _context.PolicyOverrides.AsQueryable();

            if (!string.IsNullOrEmpty(organization))
                query = query.Where(o => o.Organization == organization);

            if (!string.IsNullOrEmpty(project))
                query = query.Where(o => o.Project == project);

            if (activeOnly)
                query = query.Where(o => o.IsActive && (o.ExpiresAt == null || o.ExpiresAt > DateTime.UtcNow));

            var overrides = await query
                .OrderByDescending(o => o.RequestedAt)
                .ToListAsync();

            // Deserialize metadata from JSON
            foreach (var policyOverride in overrides)
            {
                try
                {
                    policyOverride.Metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(policyOverride.MetadataJson) ?? new();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize metadata for policy override {Id}", policyOverride.Id);
                    policyOverride.Metadata = new();
                }
            }

            return overrides;
        }

        public async Task<SecurityMetricsData> GetSecurityMetricsAsync(
            DateTime startDate,
            DateTime endDate,
            string? organization = null,
            string? project = null)
        {
            var events = await GetSecurityEventsAsync(startDate, endDate, organization, project);

            var findingsBySeverity = events
                .Where(e => e.EventType == "finding_created")
                .GroupBy(e => e.Properties.GetValueOrDefault("severity", "Unknown").ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var findingsByStatus = events
                .Where(e => e.EventType == "finding_status_changed")
                .GroupBy(e => e.Properties.GetValueOrDefault("newStatus", "Unknown").ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var findingsByCategory = events
                .Where(e => e.EventType == "finding_created")
                .GroupBy(e => e.Properties.GetValueOrDefault("category", "Unknown").ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var totalOverrides = events.Count(e => e.EventType == "policy_override_requested");
            var totalRiskAcceptances = await _context.RiskAcceptances.CountAsync(ra =>
                ra.AcceptedAt >= startDate && ra.AcceptedAt <= endDate &&
                (string.IsNullOrEmpty(organization) || ra.Organization == organization) &&
                (string.IsNullOrEmpty(project) || ra.Project == project) &&
                ra.IsActive);

            return new SecurityMetricsData
            {
                TotalFindings = events.Count(e => e.EventType == "finding_created"),
                FindingsBySeverity = findingsBySeverity,
                FindingsByStatus = findingsByStatus,
                FindingsByCategory = findingsByCategory,
                TotalOverrides = totalOverrides,
                TotalRiskAcceptances = totalRiskAcceptances
            };
        }

        public async Task AddAnalysisMetadataAsync(AnalysisMetadataEntity analysisMetadata)
        {
            // Serialize complex properties to JSON
            analysisMetadata.ConfidenceBreakdownJson = System.Text.Json.JsonSerializer.Serialize(analysisMetadata.ConfidenceBreakdown);
            analysisMetadata.InputsUsedJson = System.Text.Json.JsonSerializer.Serialize(analysisMetadata.InputsUsed);

            await _context.AnalysisMetadata.AddAsync(analysisMetadata);
            _logger.LogInformation("Added analysis metadata for analysis: {AnalysisId}", analysisMetadata.AnalysisId);
        }

        public async Task<AnalysisMetadataEntity?> GetAnalysisMetadataAsync(string analysisId)
        {
            var entity = await _context.AnalysisMetadata
                .FirstOrDefaultAsync(m => m.AnalysisId == analysisId);

            if (entity != null)
            {
                // Deserialize JSON properties
                entity.ConfidenceBreakdown = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(entity.ConfidenceBreakdownJson) ?? new();
                entity.InputsUsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(entity.InputsUsedJson) ?? new();
            }

            return entity;
        }

        public async Task<IEnumerable<AnalysisMetadataEntity>> GetAnalysisMetadataByDateRangeAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var entities = await _context.AnalysisMetadata
                .Where(m => m.CreatedUtc >= startDate && m.CreatedUtc <= endDate)
                .OrderByDescending(m => m.CreatedUtc)
                .ToListAsync();

            // Deserialize JSON properties for each entity
            foreach (var entity in entities)
            {
                entity.ConfidenceBreakdown = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(entity.ConfidenceBreakdownJson) ?? new();
                entity.InputsUsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(entity.InputsUsedJson) ?? new();
            }

            return entities;
        }

        public async Task<Dictionary<string, string>> GetCurrentVersionsAsync()
        {
            // Get the most recent versions from analysis metadata
            var latestMetadata = await _context.AnalysisMetadata
                .OrderByDescending(m => m.CreatedUtc)
                .FirstOrDefaultAsync();

            return new Dictionary<string, string>
            {
                ["PromptVersion"] = latestMetadata?.PromptVersion ?? "v1.0",
                ["PolicyVersion"] = latestMetadata?.PolicyVersion ?? "v1.0"
            };
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}