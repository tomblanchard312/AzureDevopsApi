using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ADOApi.Data;
using ADOApi.Models.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ADOApi.Services
{
    public class WorkItemProposalService
    {
        private readonly SecurityAdvisorDbContext _context;
        private readonly ILogger<WorkItemProposalService> _logger;

        // Allowed work item types for proposals
        private readonly HashSet<string> _allowedWorkItemTypes = new HashSet<string>
        {
            "Bug",
            "Task",
            "User Story",
            "Feature",
            "Epic",
            "Issue",
            "Product Backlog Item"
        };

        public WorkItemProposalService(
            SecurityAdvisorDbContext context,
            ILogger<WorkItemProposalService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<int>> AddChatProposalsAsync(string repoKey, string chatMessageId, List<WorkItemProposal> proposals)
        {
            if (string.IsNullOrWhiteSpace(repoKey))
                throw new ArgumentException("Repository key cannot be null or empty", nameof(repoKey));

            if (string.IsNullOrWhiteSpace(chatMessageId))
                throw new ArgumentException("Chat message ID cannot be null or empty", nameof(chatMessageId));

            if (proposals == null || !proposals.Any())
                throw new ArgumentException("Proposals list cannot be null or empty", nameof(proposals));

            var persistedProposalIds = new List<int>();

            foreach (var proposal in proposals)
            {
                // Validate proposal
                if (proposal.Confidence < 0.8)
                {
                    _logger.LogWarning("Proposal rejected due to low confidence: {Confidence} for repo {RepoKey}",
                        proposal.Confidence, repoKey);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(proposal.WorkItemType) || !_allowedWorkItemTypes.Contains(proposal.WorkItemType))
                {
                    _logger.LogWarning("Proposal rejected due to invalid work item type: {WorkItemType} for repo {RepoKey}",
                        proposal.WorkItemType, repoKey);
                    continue;
                }

                // Create and persist the proposal
                var proposalEntity = new WorkItemProposalEntity
                {
                    RepoKey = repoKey,
                    ChatMessageId = chatMessageId,
                    Title = proposal.Title,
                    Description = proposal.Description,
                    WorkItemType = proposal.WorkItemType,
                    Confidence = proposal.Confidence,
                    Source = "Chat",
                    Status = "Proposed",
                    CreatedAt = DateTime.UtcNow,
                    Metadata = proposal.Metadata ?? new Dictionary<string, object>()
                };

                _context.WorkItemProposals.Add(proposalEntity);
                await _context.SaveChangesAsync();

                // Create AgentDecision record
                var agentDecision = new AgentDecision
                {
                    RepoKey = repoKey,
                    RunId = Guid.Empty, // Use empty GUID for chat-generated decisions without a specific run
                    DecisionType = "WorkItemProposal",
                    TargetType = "Proposal",
                    TargetId = proposalEntity.Id.ToString(),
                    Decision = "Allow", // Chat proposals are allowed by default
                    Justification = $"Chat-generated work item proposal with confidence {proposal.Confidence:P2}",
                    Confidence = proposal.Confidence,
                    CreatedBy = "Chat",
                    CreatedUtc = DateTime.UtcNow
                };

                _context.AgentDecisions.Add(agentDecision);
                await _context.SaveChangesAsync();

                persistedProposalIds.Add(proposalEntity.Id);

                _logger.LogInformation("Created work item proposal {ProposalId} for repo {RepoKey} from chat message {ChatMessageId}",
                    proposalEntity.Id, repoKey, chatMessageId);
            }

            return persistedProposalIds;
        }

        public async Task<List<int>> AddChatProposalsAsync(string repoKey, Guid chatMessageId, IEnumerable<WorkItemProposalDraft> proposals)
        {
            if (string.IsNullOrWhiteSpace(repoKey))
                throw new ArgumentException("Repository key cannot be null or empty", nameof(repoKey));

            if (proposals == null || !proposals.Any())
                throw new ArgumentException("Proposals list cannot be null or empty", nameof(proposals));

            var persistedProposalIds = new List<int>();

            foreach (var proposal in proposals)
            {
                // Validate confidence >= configured threshold (0.8)
                if (proposal.Confidence < 0.8)
                {
                    _logger.LogWarning("Proposal rejected due to low confidence: {Confidence} for repo {RepoKey}",
                        proposal.Confidence, repoKey);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(proposal.WorkItemType) || !_allowedWorkItemTypes.Contains(proposal.WorkItemType))
                {
                    _logger.LogWarning("Proposal rejected due to invalid work item type: {WorkItemType} for repo {RepoKey}",
                        proposal.WorkItemType, repoKey);
                    continue;
                }

                // Create and persist the proposal
                var proposalEntity = new WorkItemProposalEntity
                {
                    RepoKey = repoKey,
                    ChatMessageId = chatMessageId.ToString(),
                    Title = proposal.Title,
                    Description = proposal.Description,
                    WorkItemType = proposal.WorkItemType,
                    Confidence = proposal.Confidence,
                    Source = "Chat",
                    Status = "Proposed",
                    CreatedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["AcceptanceCriteria"] = proposal.AcceptanceCriteria,
                        ["Rationale"] = proposal.Rationale
                    }
                };

                _context.WorkItemProposals.Add(proposalEntity);
                await _context.SaveChangesAsync();

                // Create AgentDecision record
                var agentDecision = new AgentDecision
                {
                    RepoKey = repoKey,
                    RunId = Guid.Empty, // Use empty GUID for chat-generated decisions without a specific run
                    DecisionType = "WorkItemProposal",
                    TargetType = "Proposal",
                    TargetId = proposalEntity.Id.ToString(),
                    Decision = "Allow", // Chat proposals are allowed by default
                    Justification = $"Chat-generated work item proposal with confidence {proposal.Confidence:P2}",
                    Confidence = proposal.Confidence,
                    CreatedBy = "Chat",
                    CreatedUtc = DateTime.UtcNow
                };

                _context.AgentDecisions.Add(agentDecision);
                await _context.SaveChangesAsync();

                persistedProposalIds.Add(proposalEntity.Id);

                _logger.LogInformation("Created work item proposal {ProposalId} for repo {RepoKey} from chat message {ChatMessageId}",
                    proposalEntity.Id, repoKey, chatMessageId);
            }

            return persistedProposalIds;
        }
    }

    // Models for the service
    public class WorkItemProposal
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string WorkItemType { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}