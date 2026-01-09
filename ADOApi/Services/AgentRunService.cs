using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ADOApi.Services
{
    public class AgentRunService : IAgentRunService
    {
        private readonly SecurityAdvisorDbContext _context;
        private readonly ILogger<AgentRunService> _logger;

        public AgentRunService(SecurityAdvisorDbContext context, ILogger<AgentRunService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AgentRun> StartRunAsync(string repoKey, string runType, string modelProvider, string modelName, string promptVersion, string policyVersion, string? correlationId = null)
        {
            var run = new AgentRun
            {
                RepoKey = repoKey,
                RunType = runType,
                ModelProvider = modelProvider,
                ModelName = modelName,
                PromptVersion = promptVersion,
                PolicyVersion = policyVersion,
                CorrelationId = correlationId,
                StartedUtc = DateTime.UtcNow,
                Status = "Started"
            };

            _context.AgentRuns.Add(run);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Started agent run {RunId} for {RunType} on {RepoKey}", run.Id, runType, repoKey);
            return run;
        }

        public async Task CompleteRunAsync(Guid runId, string status, string? outputSummaryJson = null, string? error = null)
        {
            var run = await _context.AgentRuns.FindAsync(runId);
            if (run == null)
                throw new KeyNotFoundException($"Agent run {runId} not found");

            run.Status = status;
            run.CompletedUtc = DateTime.UtcNow;
            run.OutputSummaryJson = outputSummaryJson;
            run.Error = error;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Completed agent run {RunId} with status {Status}", runId, status);
        }

        public async Task<AgentDecision> RecordDecisionAsync(Guid runId, string repoKey, string decisionType, string targetType, string targetId, string decision, string justification, double confidence, string createdBy)
        {
            var decisionEntity = new AgentDecision
            {
                RepoKey = repoKey,
                RunId = runId,
                DecisionType = decisionType,
                TargetType = targetType,
                TargetId = targetId,
                Decision = decision,
                Justification = justification,
                Confidence = confidence,
                CreatedBy = createdBy,
                CreatedUtc = DateTime.UtcNow
            };

            _context.AgentDecisions.Add(decisionEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Recorded decision for run {RunId}: {DecisionType} - {Decision}", runId, decisionType, decision);
            return decisionEntity;
        }

        public async Task<IEnumerable<AgentRun>> GetRunsAsync(string repoKey, string? runType = null, string? status = null, DateTime? since = null)
        {
            var query = _context.AgentRuns.Where(r => r.RepoKey == repoKey);

            if (!string.IsNullOrEmpty(runType))
                query = query.Where(r => r.RunType == runType);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            if (since.HasValue)
                query = query.Where(r => r.StartedUtc >= since.Value);

            query = query.OrderByDescending(r => r.StartedUtc);

            return await query.ToListAsync();
        }

        public async Task<AgentRun?> GetRunAsync(Guid runId)
        {
            return await _context.AgentRuns.FindAsync(runId);
        }

        public async Task<IEnumerable<AgentDecision>> GetDecisionsAsync(Guid runId)
        {
            return await _context.AgentDecisions.Where(d => d.RunId == runId).OrderBy(d => d.CreatedUtc).ToListAsync();
        }

        public async Task<IEnumerable<AgentDecision>> GetDecisionsByTypeAsync(string repoKey, string decisionType, DateTime? since = null)
        {
            var query = _context.AgentDecisions
                .Join(_context.AgentRuns, d => d.RunId, r => r.Id, (d, r) => new { Decision = d, Run = r })
                .Where(dr => dr.Run.RepoKey == repoKey && dr.Decision.DecisionType == decisionType);

            if (since.HasValue)
                query = query.Where(dr => dr.Decision.CreatedUtc >= since.Value);

            return await query.Select(dr => dr.Decision).OrderByDescending(d => d.CreatedUtc).ToListAsync();
        }
    }
}