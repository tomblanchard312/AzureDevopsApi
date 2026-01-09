using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ADOApi.Services
{
    public class WorkItemLinkService : IWorkItemLinkService
    {
        private readonly SecurityAdvisorDbContext _context;
        private readonly ILogger<WorkItemLinkService> _logger;

        public WorkItemLinkService(SecurityAdvisorDbContext context, ILogger<WorkItemLinkService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WorkItemLink> CreateProposalAsync(string repoKey, Guid insightId, string workItemType, string createdBy)
        {
            var proposal = new WorkItemLink
            {
                RepoKey = repoKey,
                InsightId = insightId,
                WorkItemType = workItemType,
                Disposition = "Proposed",
                CreatedBy = createdBy,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            _context.WorkItemLinks.Add(proposal);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created work item proposal for insight {InsightId} by {CreatedBy}", insightId, createdBy);
            return proposal;
        }

        public async Task<WorkItemLink> AcceptProposalAsync(Guid proposalId, int workItemId, string acceptedBy)
        {
            var proposal = await _context.WorkItemLinks.FindAsync(proposalId);
            if (proposal == null)
                throw new KeyNotFoundException($"Proposal {proposalId} not found");

            if (proposal.Disposition != "Proposed")
                throw new InvalidOperationException($"Proposal {proposalId} is not in Proposed state");

            proposal.WorkItemId = workItemId;
            proposal.Disposition = "Accepted";
            proposal.UpdatedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Accepted proposal {ProposalId} as work item {WorkItemId} by {AcceptedBy}", proposalId, workItemId, acceptedBy);
            return proposal;
        }

        public async Task RejectProposalAsync(Guid proposalId, string reason, string rejectedBy)
        {
            var proposal = await _context.WorkItemLinks.FindAsync(proposalId);
            if (proposal == null)
                throw new KeyNotFoundException($"Proposal {proposalId} not found");

            if (proposal.Disposition != "Proposed")
                throw new InvalidOperationException($"Proposal {proposalId} is not in Proposed state");

            proposal.Disposition = "Rejected";
            proposal.Reason = reason;
            proposal.UpdatedUtc = DateTime.UtcNow;
            proposal.ClosedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Rejected proposal {ProposalId} by {RejectedBy}: {Reason}", proposalId, rejectedBy, reason);
        }

        public async Task UpdateWorkItemStateAsync(Guid linkId, string state, string updatedBy)
        {
            var link = await _context.WorkItemLinks.FindAsync(linkId);
            if (link == null)
                throw new KeyNotFoundException($"Work item link {linkId} not found");

            link.State = state;
            link.UpdatedUtc = DateTime.UtcNow;

            if (state == "Closed" || state == "Done" || state == "Resolved")
            {
                link.Disposition = "Closed";
                link.ClosedUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated work item {WorkItemId} state to {State} by {UpdatedBy}", link.WorkItemId, state, updatedBy);
        }

        public async Task CloseWorkItemLinkAsync(Guid linkId, string closedBy)
        {
            var link = await _context.WorkItemLinks.FindAsync(linkId);
            if (link == null)
                throw new KeyNotFoundException($"Work item link {linkId} not found");

            link.Disposition = "Closed";
            link.ClosedUtc = DateTime.UtcNow;
            link.UpdatedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Closed work item link {LinkId} by {ClosedBy}", linkId, closedBy);
        }

        public async Task<IEnumerable<WorkItemLink>> GetProposalsAsync(string repoKey, string? disposition = null)
        {
            var query = _context.WorkItemLinks.Where(w => w.RepoKey == repoKey);

            if (!string.IsNullOrEmpty(disposition))
                query = query.Where(w => w.Disposition == disposition);
            else
                query = query.Where(w => w.Disposition == "Proposed" || w.Disposition == "Accepted");

            return await query.OrderByDescending(w => w.CreatedUtc).ToListAsync();
        }

        public async Task<WorkItemLink?> GetLinkByWorkItemIdAsync(int workItemId)
        {
            return await _context.WorkItemLinks.FirstOrDefaultAsync(w => w.WorkItemId == workItemId);
        }
    }
}