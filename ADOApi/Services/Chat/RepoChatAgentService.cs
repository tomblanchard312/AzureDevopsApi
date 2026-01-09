using System.Text.Json;
using ADOApi.Interfaces;
using ADOApi.Models.Chat;

namespace ADOApi.Services.Chat
{
    public class RepoChatAgentService
    {
        private readonly ILLMClient _llmClient;

        public RepoChatAgentService(ILLMClient llmClient)
        {
            _llmClient = llmClient;
        }

        public async Task<RepoChatResponse> RunChatAsync(RepoChatRequest request, ChatContext context)
        {
            // Build context string from ChatContext
            var contextString = BuildContextString(request, context);

            // Create system prompt that forbids side effects and enforces mode rules
            var systemPrompt = BuildSystemPrompt(request.Mode);

            // Create user prompt with request and context
            var userPrompt = BuildUserPrompt(request, contextString);

            // Call LLM
            var llmResponse = await _llmClient.GenerateAsync(systemPrompt, userPrompt);

            // Parse JSON response into RepoChatResponse
            return ParseLlmResponse(llmResponse, request.Mode);
        }

        private string BuildContextString(RepoChatRequest request, ChatContext context)
        {
            var contextParts = new List<string>();

            // Add repository information
            contextParts.Add($"Repository: {context.RepositoryName}");
            contextParts.Add($"Organization: {context.OrganizationName}");
            contextParts.Add($"Project: {context.ProjectName}");

            // Add file context if provided
            if (request.Context.FilePaths.Length > 0)
            {
                contextParts.Add($"Requested Files: {string.Join(", ", request.Context.FilePaths)}");
            }

            // Add commit context if provided
            if (!string.IsNullOrEmpty(request.Context.CommitSha))
            {
                contextParts.Add($"Commit SHA: {request.Context.CommitSha}");
            }

            // Add insight context if provided
            if (request.Context.InsightIds.Length > 0)
            {
                contextParts.Add($"Insight IDs: {string.Join(", ", request.Context.InsightIds)}");
            }

            // Add repository files (limited to avoid token limits)
            if (context.RepositoryFiles.Count > 0)
            {
                var fileSummaries = context.RepositoryFiles
                    .Take(10) // Limit to 10 files
                    .Select(kvp => $"{kvp.Key}: {kvp.Value?.Substring(0, Math.Min(200, kvp.Value?.Length ?? 0))}...");
                contextParts.Add($"Repository Files:\n{string.Join("\n", fileSummaries)}");
            }

            // Add recent commits
            if (context.RecentCommits.Count > 0)
            {
                contextParts.Add($"Recent Commits:\n{string.Join("\n", context.RecentCommits.Take(5))}");
            }

            // Add active work items
            if (context.ActiveWorkItems.Count > 0)
            {
                contextParts.Add($"Active Work Items:\n{string.Join("\n", context.ActiveWorkItems.Take(5))}");
            }

            return string.Join("\n\n", contextParts);
        }

        private string BuildSystemPrompt(string mode)
        {
            var basePrompt = @"You are a repository-aware AI assistant that helps developers understand and work with code repositories.

IMPORTANT RULES:
- You MUST respond with valid JSON only
- You MUST NOT perform any side effects or actions
- You MUST NOT create, modify, or delete any files, work items, or data
- You MUST NOT persist any information or memory
- You MUST NOT make API calls or external requests

RESPONSE FORMAT:
You must respond with a JSON object matching this exact structure:
{
  ""reply"": ""string - your helpful response"",
  ""confidence"": number - confidence score 0.0-1.0,
  ""sources"": [array of {""type"": ""string"", ""value"": ""string""}],
  ""proposals"": [array of work item proposal objects],
  ""notes"": ""string or null""
}

WORK ITEM PROPOSAL FORMAT:
{
  ""workItemType"": ""string (Bug|Task|User Story|Feature|Epic)"",
  ""title"": ""string"",
  ""description"": ""string"",
  ""acceptanceCriteria"": [array of strings],
  ""confidence"": number 0.0-1.0,
  ""rationale"": ""string""
}";

            // Add mode-specific rules
            if (mode == "Plan")
            {
                basePrompt += "\n\nMODE: Plan - You MAY include work item proposals in the proposals array if they would help implement the user's request.";
            }
            else
            {
                basePrompt += "\n\nMODE: " + mode + " - You MUST NOT include any work item proposals. Set proposals to an empty array.";
            }

            return basePrompt;
        }

        private string BuildUserPrompt(RepoChatRequest request, string contextString)
        {
            return $@"
Context Information:
{contextString}

User Request:
{request.Message}

Please provide a helpful response based on the repository context above.";
        }

        private RepoChatResponse ParseLlmResponse(string llmResponse, string mode)
        {
            try
            {
                // Try to parse the JSON response
                var response = JsonSerializer.Deserialize<RepoChatResponse>(llmResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (response == null)
                {
                    throw new JsonException("Response was null");
                }

                // Enforce mode rules - ensure no proposals unless in Plan mode
                if (mode != "Plan" && response.Proposals.Any())
                {
                    response.Proposals.Clear();
                    response.Notes = (response.Notes ?? "") + " (Proposals filtered due to mode restrictions)";
                }

                // Validate confidence ranges
                response.Confidence = Math.Clamp(response.Confidence, 0.0, 1.0);
                foreach (var proposal in response.Proposals)
                {
                    proposal.Confidence = Math.Clamp(proposal.Confidence, 0.0, 1.0);
                }

                return response;
            }
            catch (JsonException ex)
            {
                // If JSON parsing fails, return a fallback response
                return new RepoChatResponse
                {
                    Reply = "I apologize, but I encountered an error processing your request. Please try again.",
                    Confidence = 0.0,
                    Sources = new List<ChatSource>(),
                    Proposals = new List<WorkItemProposalDraft>(),
                    Notes = $"JSON parsing error: {ex.Message}"
                };
            }
        }
    }
}