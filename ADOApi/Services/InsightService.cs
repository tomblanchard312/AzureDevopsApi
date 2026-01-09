using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ADOApi.Services
{
    public class InsightService : IInsightService
    {
        private readonly SecurityAdvisorDbContext _context;
        private readonly ILogger<InsightService> _logger;

        public InsightService(SecurityAdvisorDbContext context, ILogger<InsightService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<CodeInsight>> UpsertInsightsAsync(string repoKey, IEnumerable<CodeInsight> insights)
        {
            var upsertedInsights = new List<CodeInsight>();

            foreach (var insight in insights)
            {
                insight.RepoKey = repoKey;
                insight.LastSeenUtc = DateTime.UtcNow;

                var existing = await _context.CodeInsights
                    .FirstOrDefaultAsync(i => i.RepoKey == repoKey && i.Fingerprint == insight.Fingerprint);

                if (existing != null)
                {
                    // Update existing insight
                    existing.LastSeenUtc = DateTime.UtcNow;
                    existing.Confidence = Math.Max(existing.Confidence, insight.Confidence); // Keep highest confidence
                    existing.EvidenceJson = insight.EvidenceJson ?? existing.EvidenceJson;
                    existing.RelatedMemoryIdsJson = insight.RelatedMemoryIdsJson ?? existing.RelatedMemoryIdsJson;

                    upsertedInsights.Add(existing);
                }
                else
                {
                    // Create new insight
                    insight.FirstSeenUtc = DateTime.UtcNow;
                    _context.CodeInsights.Add(insight);
                    upsertedInsights.Add(insight);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Upserted {Count} insights for {RepoKey}", upsertedInsights.Count, repoKey);
            return upsertedInsights;
        }

        public async Task<CodeInsight?> GetInsightAsync(Guid id)
        {
            return await _context.CodeInsights.FindAsync(id);
        }

        public async Task<IEnumerable<CodeInsight>> GetInsightsAsync(string repoKey, string? status = null, string? severity = null, string? insightType = null)
        {
            var query = _context.CodeInsights.Where(i => i.RepoKey == repoKey);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(i => i.Status == status);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(i => i.Severity == severity);

            if (!string.IsNullOrEmpty(insightType))
                query = query.Where(i => i.InsightType == insightType);

            return await query.OrderByDescending(i => i.LastSeenUtc).ToListAsync();
        }

        public async Task UpdateInsightStatusAsync(Guid id, string status, string userId)
        {
            var insight = await _context.CodeInsights.FindAsync(id);
            if (insight == null)
                throw new KeyNotFoundException($"Insight {id} not found");

            insight.Status = status;
            insight.LastSeenUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated insight {Id} status to {Status} by {UserId}", id, status, userId);
        }

        public async Task MarkInsightFixedAsync(Guid id, string userId)
        {
            await UpdateInsightStatusAsync(id, "Fixed", userId);
        }

        public async Task MarkInsightAcceptedAsync(Guid id, string userId)
        {
            await UpdateInsightStatusAsync(id, "Accepted", userId);
        }

        public async Task MarkInsightSuppressedAsync(Guid id, string userId)
        {
            await UpdateInsightStatusAsync(id, "Suppressed", userId);
        }
    }
}