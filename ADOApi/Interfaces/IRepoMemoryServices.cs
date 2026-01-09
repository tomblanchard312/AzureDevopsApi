using System.Threading.Tasks;
using System.Collections.Generic;
using ADOApi.Data;

namespace ADOApi.Interfaces
{
    public interface IRepoMemoryService
    {
        Task<RepoSnapshot> CreateSnapshotAsync(string repoKey, string branch, string commitSha, string? summary, string? statsJson);
        Task<RepositoryMemory> CreateMemoryAsync(string repoKey, string memoryType, string title, string content, string source, double confidence, string userId);
        Task<RepositoryMemory?> GetMemoryAsync(Guid id);
        Task<IEnumerable<RepositoryMemory>> GetMemoriesAsync(string repoKey, string? memoryType = null, bool activeOnly = true);
        Task<RepositoryMemory> UpdateMemoryAsync(Guid id, string title, string content, double confidence, string userId);
        Task DeactivateMemoryAsync(Guid id, string userId);
        Task<bool> ValidateMemoryAsync(Guid id);
    }

    public interface IInsightService
    {
        Task<IEnumerable<CodeInsight>> UpsertInsightsAsync(string repoKey, IEnumerable<CodeInsight> insights);
        Task<CodeInsight?> GetInsightAsync(Guid id);
        Task<IEnumerable<CodeInsight>> GetInsightsAsync(string repoKey, string? status = null, string? severity = null, string? insightType = null);
        Task UpdateInsightStatusAsync(Guid id, string status, string userId);
        Task MarkInsightFixedAsync(Guid id, string userId);
        Task MarkInsightAcceptedAsync(Guid id, string userId);
        Task MarkInsightSuppressedAsync(Guid id, string userId);
    }

    public interface IWorkItemLinkService
    {
        Task<WorkItemLink> CreateProposalAsync(string repoKey, Guid insightId, string workItemType, string createdBy);
        Task<WorkItemLink> AcceptProposalAsync(Guid proposalId, int workItemId, string acceptedBy);
        Task RejectProposalAsync(Guid proposalId, string reason, string rejectedBy);
        Task UpdateWorkItemStateAsync(Guid linkId, string state, string updatedBy);
        Task CloseWorkItemLinkAsync(Guid linkId, string closedBy);
        Task<IEnumerable<WorkItemLink>> GetProposalsAsync(string repoKey, string? disposition = null);
        Task<WorkItemLink?> GetLinkByWorkItemIdAsync(int workItemId);
    }

    public interface IAgentRunService
    {
        Task<AgentRun> StartRunAsync(string repoKey, string runType, string modelProvider, string modelName, string promptVersion, string policyVersion, string? correlationId = null);
        Task CompleteRunAsync(Guid runId, string status, string? outputSummaryJson = null, string? error = null);
        Task<AgentRun> StartChatRunAsync(string repoKey, string modelProvider, string promptVersion, string policyVersion, string mode, int fileCount, string? correlationId = null);
        Task CompleteChatRunAsync(Guid runId, string status, int proposalCount, string? error = null);
        Task<AgentDecision> RecordDecisionAsync(Guid runId, string repoKey, string decisionType, string targetType, string targetId, string decision, string justification, double confidence, string createdBy);
        Task<IEnumerable<AgentRun>> GetRunsAsync(string repoKey, string? runType = null, string? status = null, DateTime? since = null);
        Task<AgentRun?> GetRunAsync(Guid runId);
    }

    public interface IFingerprintService
    {
        string CreateFingerprint(string ruleId, string? filePath, string? codeSnippet, string message);
        string CreateFingerprint(string ruleId, string filePath, int? startLine, int? endLine, string message);
    }
}