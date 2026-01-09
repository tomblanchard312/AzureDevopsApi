using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Polly;
using Polly.Retry;
using Microsoft.VisualStudio.Services.Common;
using System;

namespace ADOApi.Services
{
    public class PullRequestCommentService : IPullRequestCommentService
    {
        private readonly ILLMClient _llmClient;
        private readonly ISecurityAdvisorService _securityAdvisorService;
        private readonly ILogger<PullRequestCommentService> _logger;
        private readonly GitHttpClient _gitClient;
        private readonly AsyncRetryPolicy _retryPolicy;

        public PullRequestCommentService(
            ILLMClient llmClient,
            ISecurityAdvisorService securityAdvisorService,
            ILogger<PullRequestCommentService> logger,
            VssConnection connection)
        {
            _llmClient = llmClient;
            _securityAdvisorService = securityAdvisorService;
            _logger = logger;
            _gitClient = connection.GetClient<GitHttpClient>();

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, "Retry {RetryCount} after {TimeSpan} for PR comment operation", retryCount, timeSpan);
                    });
        }

        public async Task<PullRequestCommentResponse> GenerateAndPostCommentAsync(PullRequestCommentRequest request)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await PreviewCommentAsync(request);
                if (!response.Success || request.PreviewOnly)
                {
                    return response;
                }

                // Post the comment to Azure DevOps
                var thread = await PostCommentToPullRequestAsync(request, response.CommentMarkdown!);
                response.ThreadId = thread.Id;
                response.CommentUrl = GetCommentUrl(request, thread.Id);
                response.PostedAt = DateTime.UtcNow;

                _logger.LogInformation("Posted security review comment to PR {PRId} in thread {ThreadId}",
                    request.PullRequestId, thread.Id);

                return response;
            });
        }

        public async Task<PullRequestCommentResponse> PreviewCommentAsync(PullRequestCommentRequest request)
        {
            try
            {
                // Get security findings
                var findings = await GetFindingsForCommentAsync(request.FindingIds);

                // Generate comment using LLM
                var commentMarkdown = await GenerateCommentMarkdownAsync(findings, request);

                return new PullRequestCommentResponse
                {
                    Success = true,
                    CommentMarkdown = commentMarkdown
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preview PR comment for PR {PRId}", request.PullRequestId);
                return new PullRequestCommentResponse
                {
                    Success = false,
                    ErrorMessage = $"Failed to generate comment: {ex.Message}"
                };
            }
        }

        public async Task<PullRequestCommentResponse> UpdateCommentAsync(int threadId, PullRequestCommentRequest request)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await PreviewCommentAsync(request);
                if (!response.Success)
                {
                    return response;
                }

                // Update existing thread
                await UpdateCommentInThreadAsync(request, threadId, response.CommentMarkdown!);
                response.ThreadId = threadId;
                response.CommentUrl = GetCommentUrl(request, threadId);
                response.PostedAt = DateTime.UtcNow;

                _logger.LogInformation("Updated security review comment in PR {PRId} thread {ThreadId}",
                    request.PullRequestId, threadId);

                return response;
            });
        }

        public async Task<InlineCommentResponse> PostInlineCommentAsync(InlineCommentRequest request)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await PreviewInlineCommentAsync(request);
                if (!response.Success || request.PreviewOnly)
                {
                    return response;
                }

                // Post the inline comment to Azure DevOps
                var thread = await PostInlineCommentToPullRequestAsync(request, response.CommentMarkdown!);
                response.ThreadId = thread.Id;
                response.CommentUrl = GetCommentUrl(new PullRequestCommentRequest
                {
                    Organization = request.Organization,
                    Project = request.Project,
                    PullRequestId = request.PullRequestId,
                    RepositoryId = request.RepositoryId
                }, thread.Id);
                response.PostedAt = DateTime.UtcNow;

                _logger.LogInformation("Posted inline security comment to PR {PRId} in thread {ThreadId}",
                    request.PullRequestId, thread.Id);

                return response;
            });
        }

        public async Task<ThreadResolutionResponse> ResolveFixedThreadsAsync(ThreadResolutionRequest request)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = new ThreadResolutionResponse { Success = true };
                var allFindings = await _securityAdvisorService.GetFindingsAsync();
                var openFindings = allFindings.Where(f => f.Status == "Open").ToList();

                // Get all threads for this PR (or specific threads if provided)
                var threads = await GetPullRequestThreadsAsync(request);
                var securityThreads = threads.Where(t => IsSecurityThread(t)).ToList();

                foreach (var thread in securityThreads)
                {
                    if (request.ThreadIds != null && !request.ThreadIds.Contains(thread.Id))
                        continue;

                    var findingId = ExtractFindingIdFromThread(thread);
                    if (string.IsNullOrEmpty(findingId))
                        continue;

                    // Check if finding still exists
                    var finding = openFindings.FirstOrDefault(f => f.Id == findingId);
                    if (finding == null)
                    {
                        // Finding no longer exists - resolve the thread
                        var resolutionComment = await GenerateResolutionCommentAsync(findingId);
                        await ResolveThreadAsync(request, thread.Id, resolutionComment);

                        response.ResolvedThreads.Add(new ResolvedThread
                        {
                            ThreadId = thread.Id,
                            FindingId = findingId,
                            ResolutionComment = resolutionComment,
                            ResolvedAt = DateTime.UtcNow
                        });

                        _logger.LogInformation("Resolved security thread {ThreadId} for fixed finding {FindingId}",
                            thread.Id, findingId);
                    }
                }

                return response;
            });
        }

        public async Task<PrStatusResponse> PostPrStatusAsync(PrStatusRequest request)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var findings = await _securityAdvisorService.GetFindingsAsync();
                var openFindings = findings.Where(f => f.Status == "Open").ToList();

                var criticalCount = openFindings.Count(f => f.Severity == "Critical");
                var highCount = openFindings.Count(f => f.Severity == "High");

                string status;
                string description;

                if (criticalCount > 0)
                {
                    status = "failed";
                    description = $"Security check failed: {criticalCount} critical finding(s) must be addressed";
                }
                else if (highCount > 0)
                {
                    status = "failed";
                    description = $"Security check failed: {highCount} high-severity finding(s) must be addressed";
                }
                else
                {
                    status = "succeeded";
                    description = "Security check passed: No critical or high-severity findings";
                }

                await PostStatusToPullRequestAsync(request, status, description);

                return new PrStatusResponse
                {
                    Success = true,
                    Status = status,
                    Description = description,
                    StatusUrl = GetPrStatusUrl(request)
                };
            });
        }

        private async Task<List<SecurityFinding>> GetFindingsForCommentAsync(List<string>? findingIds)
        {
            var allFindings = await _securityAdvisorService.GetFindingsAsync();

            if (findingIds == null || !findingIds.Any())
            {
                // Return all open findings
                return allFindings.Where(f => f.Status == "Open").ToList();
            }

            // Return specific findings
            return allFindings.Where(f => findingIds.Contains(f.Id)).ToList();
        }

        private async Task<string> GenerateCommentMarkdownAsync(List<SecurityFinding> findings, PullRequestCommentRequest request)
        {
            if (!findings.Any())
            {
                return "## Security Review: No Open Findings\n\nNo security findings require attention at this time.";
            }

            // Prepare findings data for LLM
            var findingsData = findings.Select(f => new SecurityFindingComment
            {
                FindingId = f.Id,
                Title = f.Title,
                Severity = f.Severity,
                Description = f.Description,
                FilePath = f.FilePath,
                LineNumber = f.LineNumber,
                WhyNotFixReasons = f.Recommendations.FirstOrDefault()?.WhyNotFixReasons ?? new List<string>(),
                Recommendation = f.Recommendations.FirstOrDefault()?.Description ?? "No recommendation available"
            }).ToList();

            // Calculate confidence scores for findings with recommendations
            foreach (var finding in findingsData)
            {
                var recommendation = findings.FirstOrDefault(f => f.Id == finding.FindingId)
                    ?.Recommendations.FirstOrDefault();
                if (recommendation != null)
                {
                    finding.Confidence = recommendation.Confidence;
                    finding.ConfidenceScore = recommendation.ConfidenceScore;
                }
            }

            var reviewData = new SecurityReviewComment
            {
                Organization = request.Organization,
                Project = request.Project,
                PullRequestId = request.PullRequestId,
                RepositoryId = request.RepositoryId,
                Findings = findingsData,
                GeneratedAt = DateTime.UtcNow
            };

            // Use LLM to generate structured comment
            var prompt = BuildCommentPrompt(reviewData);
            var llmResponse = await _llmClient.GenerateAsync("", prompt);

            return ParseAndFormatComment(llmResponse, reviewData);
        }

        private string BuildCommentPrompt(SecurityReviewComment review)
        {
            var prompt = $@"Generate a comprehensive security review comment for an Azure DevOps pull request.

Context:
- Organization: {review.Organization}
- Project: {review.Project}
- Pull Request: #{review.PullRequestId}
- Repository: {review.RepositoryId}
- Generated: {review.GeneratedAt:yyyy-MM-dd HH:mm UTC}

Security Findings:
{string.Join("\n", review.Findings.Select(f => $@"
## {f.Title}
- **Severity:** {f.Severity}
- **File:** {f.FilePath}{(f.LineNumber.HasValue ? $" :{f.LineNumber}" : "")}
- **Confidence:** {f.Confidence} ({f.ConfidenceScore:P0})
- **Description:** {f.Description}
- **Recommendation:** {f.Recommendation}
{(f.WhyNotFixReasons.Any() ? $"- **Why not auto-fix:** {string.Join("; ", f.WhyNotFixReasons)}" : "")}
"))}

Requirements:
1. Use Azure DevOps-compatible Markdown
2. Group findings by severity (Critical, High, Medium, Low)
3. Include severity icons (🔴 🟡 🟠 🟢)
4. Show confidence scores and explanations
5. Include minimal code diffs where relevant
6. Explain ""why not fix"" for low-confidence items
7. Provide overall assessment
8. End with approval status requiring manual review
9. Be calm and precise
10. Never suggest auto-merging or bypassing policies

Format as a complete PR comment with sections and clear structure.";

            return prompt;
        }

        private string ParseAndFormatComment(string llmResponse, SecurityReviewComment review)
        {
            // If LLM provides well-formatted response, use it; otherwise format ourselves
            if (llmResponse.Contains("## Security Review") && llmResponse.Contains("**Severity:**"))
            {
                return llmResponse;
            }

            // Fallback formatting
            var comment = new System.Text.StringBuilder();
            comment.AppendLine("## 🔒 Security Review");
            comment.AppendLine();
            comment.AppendLine($"**Pull Request:** #{review.PullRequestId}");
            comment.AppendLine($"**Generated:** {review.GeneratedAt:yyyy-MM-dd HH:mm UTC}");
            comment.AppendLine();

            // Group by severity
            var severityGroups = review.Findings.GroupBy(f => f.Severity)
                .OrderByDescending(g => GetSeverityOrder(g.Key));

            foreach (var group in severityGroups)
            {
                var icon = GetSeverityIcon(group.Key);
                comment.AppendLine($"### {icon} {group.Key} Severity ({group.Count()})");
                comment.AppendLine();

                foreach (var finding in group)
                {
                    comment.AppendLine($"#### {finding.Title}");
                    comment.AppendLine();
                    comment.AppendLine($"**File:** {finding.FilePath}{(finding.LineNumber.HasValue ? $":{finding.LineNumber}" : "")}");
                    comment.AppendLine($"**Confidence:** {finding.Confidence} ({finding.ConfidenceScore:P0})");
                    comment.AppendLine();
                    comment.AppendLine(finding.Description);
                    comment.AppendLine();
                    comment.AppendLine($"**Recommendation:** {finding.Recommendation}");
                    if (finding.WhyNotFixReasons.Any())
                    {
                        comment.AppendLine();
                        comment.AppendLine($"**Why not auto-fix:** {string.Join("; ", finding.WhyNotFixReasons)}");
                    }
                    comment.AppendLine();
                }
            }

            // Overall assessment
            var criticalCount = review.Findings.Count(f => f.Severity == "Critical");
            var highCount = review.Findings.Count(f => f.Severity == "High");

            comment.AppendLine("### 📊 Overall Assessment");
            comment.AppendLine();
            if (criticalCount > 0 || highCount > 0)
            {
                comment.AppendLine($"⚠️ **{criticalCount + highCount} high-priority findings** require immediate attention.");
            }
            else
            {
                comment.AppendLine("✅ No critical or high-severity findings detected.");
            }
            comment.AppendLine();
            comment.AppendLine("### ✅ Approval Status");
            comment.AppendLine();
            comment.AppendLine("**Requires manual review and approval.** All security findings must be addressed or explicitly accepted before merge.");

            return comment.ToString();
        }

        private async Task<GitPullRequestCommentThread> PostCommentToPullRequestAsync(PullRequestCommentRequest request, string commentMarkdown)
        {
            var comment = new Comment
            {
                Content = commentMarkdown,
                CommentType = CommentType.Text
            };

            var thread = new GitPullRequestCommentThread
            {
                Comments = new List<Comment> { comment },
                ThreadContext = new CommentThreadContext
                {
                    FilePath = "/SecurityReview.md", // Virtual file for the review
                    RightFileStart = new CommentPosition { Line = 1 },
                    RightFileEnd = new CommentPosition { Line = 1 }
                },
                Properties = new PropertiesCollection()
            };

            return await _gitClient.CreateThreadAsync(thread, request.RepositoryId, request.PullRequestId);
        }

        private async Task UpdateCommentInThreadAsync(PullRequestCommentRequest request, int threadId, string commentMarkdown)
        {
            var thread = await _gitClient.GetPullRequestThreadAsync(request.RepositoryId, request.PullRequestId, threadId);
            if (thread?.Comments?.Any() == true)
            {
                var comment = thread.Comments.First();
                comment.Content = commentMarkdown;

                await _gitClient.UpdateCommentAsync(comment, request.RepositoryId, request.PullRequestId, threadId, comment.Id);
            }
        }

        private string GetCommentUrl(PullRequestCommentRequest request, int threadId)
        {
            return $"https://dev.azure.com/{request.Organization}/{request.Project}/_git/{request.RepositoryId}/pullrequest/{request.PullRequestId}?_a=files&threadId={threadId}";
        }

        private int GetSeverityOrder(string severity)
        {
            return severity.ToLower() switch
            {
                "critical" => 4,
                "high" => 3,
                "medium" => 2,
                "low" => 1,
                "info" => 0,
                _ => 0
            };
        }

        private string GetSeverityIcon(string severity)
        {
            return severity.ToLower() switch
            {
                "critical" => "🔴",
                "high" => "🟡",
                "medium" => "🟠",
                "low" => "🟢",
                "info" => "ℹ️",
                _ => "❓"
            };
        }

        private async Task<InlineCommentResponse> PreviewInlineCommentAsync(InlineCommentRequest request)
        {
            try
            {
                // Get the specific finding
                var findings = await _securityAdvisorService.GetFindingsAsync();
                var finding = findings.FirstOrDefault(f => f.Id == request.FindingId);

                if (finding == null)
                {
                    return new InlineCommentResponse
                    {
                        Success = false,
                        ErrorMessage = $"Finding {request.FindingId} not found"
                    };
                }

                // Only create inline comments for high-confidence findings
                var recommendation = finding.Recommendations.FirstOrDefault();
                if (recommendation == null || recommendation.ConfidenceScore < 0.8)
                {
                    return new InlineCommentResponse
                    {
                        Success = false,
                        ErrorMessage = "Inline comments only created for high-confidence findings"
                    };
                }

                // Generate concise inline comment
                var commentMarkdown = GenerateInlineCommentMarkdown(finding, recommendation);

                return new InlineCommentResponse
                {
                    Success = true,
                    CommentMarkdown = commentMarkdown
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preview inline comment for finding {FindingId}", request.FindingId);
                return new InlineCommentResponse
                {
                    Success = false,
                    ErrorMessage = $"Failed to generate inline comment: {ex.Message}"
                };
            }
        }

        private async Task<GitPullRequestCommentThread> PostInlineCommentToPullRequestAsync(InlineCommentRequest request, string commentMarkdown)
        {
            var findings = await _securityAdvisorService.GetFindingsAsync();
            var finding = findings.FirstOrDefault(f => f.Id == request.FindingId)!;

            var comment = new Comment
            {
                Content = commentMarkdown,
                CommentType = CommentType.Text
            };

            var thread = new GitPullRequestCommentThread
            {
                Comments = new List<Comment> { comment },
                ThreadContext = new CommentThreadContext
                {
                    FilePath = finding.FilePath,
                    RightFileStart = new CommentPosition { Line = finding.LineNumber ?? 1 },
                    RightFileEnd = new CommentPosition { Line = finding.LineNumber ?? 1 }
                },
                Properties = new PropertiesCollection()
            };

            return await _gitClient.CreateThreadAsync(thread, request.RepositoryId, request.PullRequestId);
        }

        private string GenerateInlineCommentMarkdown(SecurityFinding finding, SecurityRecommendation recommendation)
        {
            var icon = GetSeverityIcon(finding.Severity);
            var comment = new System.Text.StringBuilder();

            comment.AppendLine($"{icon} **{finding.Title}**");
            comment.AppendLine();
            comment.AppendLine(finding.Description);
            comment.AppendLine();
            comment.AppendLine($"**Recommendation:** {recommendation.Description}");
            comment.AppendLine();
            comment.AppendLine($"*Confidence: {recommendation.Confidence} ({recommendation.ConfidenceScore:P0})*");

            return comment.ToString();
        }

        private async Task<List<GitPullRequestCommentThread>> GetPullRequestThreadsAsync(ThreadResolutionRequest request)
        {
            var threads = await _gitClient.GetThreadsAsync(request.RepositoryId, request.PullRequestId);
            return threads.ToList();
        }

        private bool IsSecurityThread(GitPullRequestCommentThread thread)
        {
            if (thread?.Comments?.Any() != true)
                return false;

            var content = thread.Comments.First().Content ?? string.Empty;
            return content.Contains("Security") || content.Contains("🔒") || content.Contains("🔴") ||
                   content.Contains("🟡") || content.Contains("🟠") || content.Contains("🟢");
        }

        private string? ExtractFindingIdFromThread(GitPullRequestCommentThread thread)
        {
            if (thread?.Comments?.Any() != true)
                return null;

            var content = thread.Comments.First().Content ?? string.Empty;

            // Look for finding ID patterns in the comment
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Finding ID:") || line.Contains("ID:"))
                {
                    var parts = line.Split(':');
                    if (parts.Length >= 2)
                    {
                        return parts[1].Trim();
                    }
                }
            }

            return null;
        }

        private async Task<string> GenerateResolutionCommentAsync(string findingId)
        {
            return $"✅ **Security Finding Resolved**\n\nFinding `{findingId}` has been addressed and is no longer detected.\n\n*Thread automatically resolved by Security Advisor*";
        }

        private async Task ResolveThreadAsync(ThreadResolutionRequest request, int threadId, string resolutionComment)
        {
            // Add resolution comment
            var comment = new Comment
            {
                Content = resolutionComment,
                CommentType = CommentType.Text
            };

            await _gitClient.CreateCommentAsync(comment, request.RepositoryId, request.PullRequestId, threadId);

            // Mark thread as resolved (this would depend on Azure DevOps API capabilities)
            // Note: Azure DevOps may not have a direct "resolve" API, this might need to be done via status change
        }

        private async Task PostStatusToPullRequestAsync(PrStatusRequest request, string status, string description)
        {
            // This would use Azure DevOps Git Status API
            // For now, we'll implement a placeholder that could be extended
            var gitStatus = new GitStatus
            {
                State = status == "succeeded" ? GitStatusState.Succeeded :
                       status == "failed" ? GitStatusState.Failed : GitStatusState.Pending,
                Description = description,
                Context = new GitStatusContext
                {
                    Name = "Security Advisor",
                    Genre = "security"
                }
            };

            if (!string.IsNullOrEmpty(request.TargetUrl))
            {
                gitStatus.TargetUrl = request.TargetUrl;
            }

            await _gitClient.CreateCommitStatusAsync(gitStatus, request.RepositoryId, request.PullRequestId.ToString());
        }

        private string GetPrStatusUrl(PrStatusRequest request)
        {
            return $"https://dev.azure.com/{request.Organization}/{request.Project}/_git/{request.RepositoryId}/pullrequest/{request.PullRequestId}";
        }
    }
}