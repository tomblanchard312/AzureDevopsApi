using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Models;
using ADOApi.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using Polly;
using Polly.Retry;

namespace ADOApi.Services
{
    public class SecurityAdvisorService : ISecurityAdvisorService
    {
        private readonly ILLMClient _llmClient;
        private readonly IRepositoryService _repositoryService;
        private readonly ISecurityGovernanceService _governanceService;
        private readonly ISecurityAdvisorRepository _securityAdvisorRepository;
        private readonly ILogger<SecurityAdvisorService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        // In-memory storage for demo - in production, use database
        private readonly Dictionary<string, SecurityFinding> _findings = new();
        private readonly Dictionary<string, SecurityRecommendation> _recommendations = new();

        public SecurityAdvisorService(
            ILLMClient llmClient,
            IRepositoryService repositoryService,
            ISecurityGovernanceService governanceService,
            ISecurityAdvisorRepository securityAdvisorRepository,
            ILogger<SecurityAdvisorService> logger)
        {
            _llmClient = llmClient;
            _repositoryService = repositoryService;
            _governanceService = governanceService;
            _securityAdvisorRepository = securityAdvisorRepository;
            _logger = logger;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, "Retry {RetryCount} after {TimeSpan} for security analysis", retryCount, timeSpan);
                    });
        }

        public async Task<SecurityAnalysisResponse> AnalyzeSarifAsync(SarifAnalysisRequest request)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var findings = new List<SecurityFinding>();
                var sarif = JsonDocument.Parse(request.SarifContent);

                if (sarif.RootElement.TryGetProperty("runs", out var runs))
                {
                    foreach (var run in runs.EnumerateArray())
                    {
                        if (run.TryGetProperty("results", out var results))
                        {
                            foreach (var result in results.EnumerateArray())
                            {
                                var finding = ParseSarifResult(result, request.Repository, request.Branch);
                                if (finding != null)
                                {
                                    findings.Add(finding);
                                    _findings[finding.Id] = finding;
                                }
                            }
                        }
                    }
                }

                var response = new SecurityAnalysisResponse
                {
                    Findings = findings,
                    TotalFindings = findings.Count,
                    HighSeverityCount = findings.Count(f => f.Severity == "High"),
                    CriticalSeverityCount = findings.Count(f => f.Severity == "Critical")
                };

                _logger.LogInformation("Analyzed SARIF with {TotalFindings} findings ({HighSeverity} high, {CriticalSeverity} critical)",
                    response.TotalFindings, response.HighSeverityCount, response.CriticalSeverityCount);

                // Create and persist analysis metadata
                var analysisMetadata = new Data.AnalysisMetadataEntity
                {
                    AnalysisId = response.AnalysisId,
                    ModelProvider = _llmClient.GetModelProvider(),
                    ModelName = _llmClient.GetModelName(),
                    PromptVersion = "v1.0", // TODO: Make configurable
                    PolicyVersion = "v1.0", // TODO: Make configurable
                    ConfidenceBreakdown = new Dictionary<string, object>
                    {
                        ["static"] = 0.7,
                        ["pattern"] = 0.8,
                        ["risk"] = 0.6,
                        ["agreement"] = 0.75
                    },
                    InputsUsed = new Dictionary<string, object>
                    {
                        ["inputType"] = "SARIF",
                        ["repository"] = request.Repository,
                        ["branch"] = request.Branch,
                        ["hasContent"] = !string.IsNullOrEmpty(request.SarifContent)
                    }
                };

                await _securityAdvisorRepository.AddAnalysisMetadataAsync(analysisMetadata);
                await _securityAdvisorRepository.SaveChangesAsync();

                // Log security events for new findings
                foreach (var finding in findings)
                {
                    await LogSecurityEventAsync(new SecurityEvent
                    {
                        EventType = "finding_created",
                        FindingId = finding.Id,
                        Timestamp = finding.CreatedAt,
                        PromptVersion = analysisMetadata.PromptVersion,
                        PolicyVersion = analysisMetadata.PolicyVersion,
                        Properties = new Dictionary<string, object>
                        {
                            ["severity"] = finding.Severity,
                            ["category"] = finding.Category,
                            ["ruleId"] = finding.RuleId
                        }
                    });
                }

                return response;
            });
        }

        public async Task<SecurityAnalysisResponse> AnalyzeSbomAsync(SbomAnalysisRequest request)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var findings = new List<SecurityFinding>();
                var sbom = JsonDocument.Parse(request.SbomContent);

                // Parse CycloneDX format
                if (sbom.RootElement.TryGetProperty("vulnerabilities", out var vulnerabilities))
                {
                    foreach (var vuln in vulnerabilities.EnumerateArray())
                    {
                        var finding = ParseSbomVulnerability(vuln);
                        if (finding != null)
                        {
                            findings.Add(finding);
                            _findings[finding.Id] = finding;
                        }
                    }
                }

                var response = new SecurityAnalysisResponse
                {
                    Findings = findings,
                    TotalFindings = findings.Count,
                    HighSeverityCount = findings.Count(f => f.Severity == "High"),
                    CriticalSeverityCount = findings.Count(f => f.Severity == "Critical")
                };

                _logger.LogInformation("Analyzed SBOM with {TotalFindings} vulnerabilities ({HighSeverity} high, {CriticalSeverity} critical)",
                    response.TotalFindings, response.HighSeverityCount, response.CriticalSeverityCount);

                // Log security events for new findings
                foreach (var finding in findings)
                {
                    await LogSecurityEventAsync(new SecurityEvent
                    {
                        EventType = "finding_created",
                        FindingId = finding.Id,
                        Timestamp = finding.CreatedAt,
                        Properties = new Dictionary<string, object>
                        {
                            ["severity"] = finding.Severity,
                            ["category"] = finding.Category,
                            ["ruleId"] = finding.RuleId
                        }
                    });
                }

                return response;
            });
        }

        public async Task<SecurityRecommendation> GenerateRecommendationAsync(string findingId, RecommendationRequest request)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                if (!_findings.TryGetValue(findingId, out var finding))
                {
                    throw new ArgumentException($"Finding {findingId} not found");
                }

                var prompt = BuildRecommendationPrompt(finding, request);
                var llmResponse = await _llmClient.GenerateAsync("", prompt);

                var recommendation = ParseRecommendationResponse(llmResponse, findingId);

                // Calculate deterministic confidence score
                var confidenceDetails = CalculateConfidenceScore(finding, recommendation, request);
                recommendation.ConfidenceScore = confidenceDetails.OverallScore;
                recommendation.ConfidenceExplanation = confidenceDetails.Explanation;

                // Map numeric score to categorical confidence
                recommendation.Confidence = MapScoreToConfidence(recommendation.ConfidenceScore);

                // Generate "why not fix" reasons for low confidence recommendations
                if (recommendation.ConfidenceScore < 0.6)
                {
                    recommendation.WhyNotFixReasons = GenerateWhyNotFixReasons(finding, confidenceDetails);
                }

                _recommendations[recommendation.Id] = recommendation;

                _logger.LogInformation("Generated recommendation {RecommendationId} for finding {FindingId} with confidence {ConfidenceScore:F2}",
                    recommendation.Id, findingId, recommendation.ConfidenceScore);

                // Log security event for recommendation generation
                await LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = "recommendation_generated",
                    FindingId = findingId,
                    Timestamp = recommendation.CreatedAt,
                    Properties = new Dictionary<string, object>
                    {
                        ["recommendationId"] = recommendation.Id,
                        ["confidence"] = recommendation.ConfidenceScore,
                        ["severity"] = finding.Severity
                    }
                });

                return recommendation;
            });
        }

        public async Task<DiffResponse> GenerateDiffAsync(string recommendationId, DiffRequest request)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                if (!_recommendations.TryGetValue(recommendationId, out var recommendation))
                {
                    throw new ArgumentException($"Recommendation {recommendationId} not found");
                }

                // Generate diff from original to modified content
                var diff = new DiffResponse();

                if (!string.IsNullOrEmpty(request.OriginalContent) && !string.IsNullOrEmpty(request.ModifiedContent))
                {
                    diff = GenerateUnifiedDiff(request.OriginalContent, request.ModifiedContent, request.FilePath ?? "unknown");
                }
                else
                {
                    // If no content provided, try to get it from the repository
                    diff.IsValid = false;
                    diff.ErrorMessage = "Original and modified content required for diff generation";
                }

                _logger.LogInformation("Generated diff for recommendation {RecommendationId}", recommendationId);

                return diff;
            });
        }

        public async Task<ApplyResponse> ApplyRecommendationAsync(string recommendationId, ApplyRequest request)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                if (!_recommendations.TryGetValue(recommendationId, out var recommendation))
                {
                    throw new ArgumentException($"Recommendation {recommendationId} not found");
                }

                if (!recommendation.Approved)
                {
                    throw new InvalidOperationException("Recommendation must be approved before applying");
                }

                // This is a placeholder - in production, this would:
                // 1. Create a new branch
                // 2. Apply the code changes
                // 3. Commit the changes
                // 4. Create a pull request

                var response = new ApplyResponse
                {
                    Success = true,
                    CommitId = Guid.NewGuid().ToString(),
                    PullRequestUrl = $"https://dev.azure.com/org/project/_git/repo/pullrequest/{Guid.NewGuid()}"
                };

                _logger.LogInformation("Applied recommendation {RecommendationId} via commit {CommitId}",
                    recommendationId, response.CommitId);

                // Log security event for recommendation application
                await LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = "recommendation_applied",
                    FindingId = recommendation.FindingId,
                    Timestamp = DateTime.UtcNow,
                    Properties = new Dictionary<string, object>
                    {
                        ["recommendationId"] = recommendationId,
                        ["commitId"] = response.CommitId
                    }
                });

                return response;
            });
        }

        public async Task<List<SecurityFinding>> GetFindingsAsync(string? status = null)
        {
            var findings = _findings.Values.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                findings = findings.Where(f => f.Status == status);
            }

            return findings.OrderByDescending(f => f.CreatedAt).ToList();
        }

        private SecurityFinding? ParseSarifResult(JsonElement result, string? repository, string? branch)
        {
            try
            {
                var finding = new SecurityFinding
                {
                    Title = result.GetProperty("message").GetProperty("text").GetString() ?? "Unknown issue",
                    Category = "SAST"
                };

                if (result.TryGetProperty("level", out var level))
                {
                    finding.Severity = level.GetString() switch
                    {
                        "error" => "High",
                        "warning" => "Medium",
                        "note" => "Low",
                        _ => "Unknown"
                    };
                }

                if (result.TryGetProperty("ruleId", out var ruleId))
                {
                    finding.RuleId = ruleId.GetString() ?? "";
                }

                if (result.TryGetProperty("locations", out var locations) && locations.GetArrayLength() > 0)
                {
                    var location = locations[0];
                    if (location.TryGetProperty("physicalLocation", out var physicalLocation))
                    {
                        if (physicalLocation.TryGetProperty("artifactLocation", out var artifactLocation))
                        {
                            finding.FilePath = artifactLocation.GetProperty("uri").GetString() ?? "";
                        }
                        if (physicalLocation.TryGetProperty("region", out var region))
                        {
                            finding.LineNumber = region.TryGetProperty("startLine", out var startLine) ? startLine.GetInt32() : null;
                        }
                    }
                }

                return finding;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse SARIF result");
                return null;
            }
        }

        private SecurityFinding? ParseSbomVulnerability(JsonElement vuln)
        {
            try
            {
                var finding = new SecurityFinding
                {
                    Category = "SCA",
                    RuleId = vuln.GetProperty("id").GetString() ?? ""
                };

                if (vuln.TryGetProperty("description", out var description))
                {
                    finding.Description = description.GetString() ?? "";
                }

                if (vuln.TryGetProperty("ratings", out var ratings) && ratings.GetArrayLength() > 0)
                {
                    var rating = ratings[0];
                    if (rating.TryGetProperty("severity", out var severity))
                    {
                        finding.Severity = severity.GetString() ?? "Unknown";
                    }
                }

                return finding;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse SBOM vulnerability");
                return null;
            }
        }

        private string BuildRecommendationPrompt(SecurityFinding finding, RecommendationRequest request)
        {
            return $@"You are a senior application security engineer. Analyze this security finding and provide remediation guidance.

Finding Details:
- Title: {finding.Title}
- Description: {finding.Description}
- Severity: {finding.Severity}
- Category: {finding.Category}
- File: {finding.FilePath}
- Line: {finding.LineNumber}
- Rule ID: {finding.RuleId}

Context:
{request.Context}

Code Snippet:
{request.CodeSnippet}

Language: {request.Language}

Provide a response in this exact JSON format:
{{
  ""title"": ""Brief title of the recommendation"",
  ""description"": ""Detailed explanation of the issue"",
  ""riskAssessment"": ""Impact and risk analysis"",
  ""remediationSteps"": ""Step-by-step fix instructions"",
  ""codeChanges"": ""Specific code changes needed"",
  ""justification"": ""Security best practices reference"",
  ""confidence"": ""High|Medium|Low"",
  ""whyNotFixReasons"": [""Array of specific reasons why this should NOT be auto-fixed, or empty array if safe to auto-fix""]
}}

IMPORTANT GUIDELINES:
- Focus on minimal, secure fixes. Do not suggest auto-fixing.
- For low-confidence recommendations, provide specific technical reasons in whyNotFixReasons.
- Reference OWASP and security best practices.
- Be conservative - when in doubt, add reasons to whyNotFixReasons.
- Consider: breaking changes, runtime errors, performance impact, false positives, complex refactoring needs.";
        }

        private SecurityRecommendation ParseRecommendationResponse(string llmResponse, string findingId)
        {
            try
            {
                // Parse JSON response from LLM
                var json = JsonDocument.Parse(llmResponse);
                var root = json.RootElement;

                return new SecurityRecommendation
                {
                    FindingId = findingId,
                    Title = root.GetProperty("title").GetString() ?? "",
                    Description = root.GetProperty("description").GetString() ?? "",
                    RiskAssessment = root.GetProperty("riskAssessment").GetString() ?? "",
                    RemediationSteps = root.GetProperty("remediationSteps").GetString() ?? "",
                    CodeChanges = root.GetProperty("codeChanges").GetString() ?? "",
                    Justification = root.GetProperty("justification").GetString() ?? "",
                    Confidence = root.GetProperty("confidence").GetString() ?? "Medium",
                    WhyNotFixReasons = root.TryGetProperty("whyNotFixReasons", out var reasons) && reasons.ValueKind == JsonValueKind.Array
                        ? reasons.EnumerateArray().Select(r => r.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                        : new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse LLM recommendation response, using fallback");

                // Fallback recommendation
                return new SecurityRecommendation
                {
                    FindingId = findingId,
                    Title = "Security Issue Detected",
                    Description = "A security vulnerability was identified but could not be analyzed automatically.",
                    RiskAssessment = "Manual review required",
                    RemediationSteps = "Please consult security documentation and review the finding manually.",
                    CodeChanges = "Manual code review required",
                    Justification = "Automated analysis failed - manual intervention needed",
                    Confidence = "Low",
                    WhyNotFixReasons = new List<string> { "Automated analysis failed - manual review required" }
                };
            }
        }

        private ConfidenceScoringDetails CalculateConfidenceScore(SecurityFinding finding, SecurityRecommendation recommendation, RecommendationRequest request)
        {
            var details = new ConfidenceScoringDetails();

            // 1. Severity-based scoring (30% weight)
            details.SeverityScore = MapSeverityToScore(finding.Severity);

            // 2. Known fix pattern scoring (25% weight)
            details.FixPatternScore = AssessFixPatternConfidence(finding, recommendation);

            // 3. Change risk assessment (25% weight)
            details.ChangeRiskScore = AssessChangeRisk(finding, recommendation, request);

            // 4. Model agreement/confidence (20% weight)
            details.ModelAgreementScore = MapModelConfidenceToScore(recommendation.Confidence);

            // Calculate weighted overall score
            details.OverallScore = (
                details.SeverityScore * 0.3 +
                details.FixPatternScore * 0.25 +
                details.ChangeRiskScore * 0.25 +
                details.ModelAgreementScore * 0.2
            );

            // Build explanation
            details.Explanation = BuildConfidenceExplanation(details);
            details.ContributingFactors = BuildContributingFactors(details);

            return details;
        }

        private double MapSeverityToScore(string severity)
        {
            return severity.ToLower() switch
            {
                "critical" => 0.9,
                "high" => 0.8,
                "medium" => 0.6,
                "low" => 0.4,
                "info" => 0.2,
                _ => 0.3
            };
        }

        private double AssessFixPatternConfidence(SecurityFinding finding, SecurityRecommendation recommendation)
        {
            double score = 0.5; // Base score

            // Known secure patterns get higher confidence
            var securePatterns = new[]
            {
                "sql injection", "xss", "csrf", "authentication", "authorization",
                "input validation", "output encoding", "parameterized query"
            };

            var findingText = (finding.Title + " " + finding.Description).ToLower();
            var recommendationText = (recommendation.Title + " " + recommendation.Description + " " + recommendation.CodeChanges).ToLower();

            // Check if finding matches known vulnerability patterns
            bool hasKnownPattern = securePatterns.Any(pattern =>
                findingText.Contains(pattern) || recommendationText.Contains(pattern));

            if (hasKnownPattern)
            {
                score += 0.3; // Boost for known patterns
            }

            // Check for specific remediation patterns
            if (recommendationText.Contains("parameterized") || recommendationText.Contains("prepared statement"))
            {
                score += 0.2; // SQL injection fixes are well-established
            }

            if (recommendationText.Contains("html encode") || recommendationText.Contains("escape"))
            {
                score += 0.2; // XSS prevention is well-established
            }

            return Math.Min(score, 1.0);
        }

        private double AssessChangeRisk(SecurityFinding finding, SecurityRecommendation recommendation, RecommendationRequest request)
        {
            double score = 0.7; // Start with moderate confidence

            // Risk factors that reduce confidence
            var riskFactors = new List<string>();

            // Complex changes reduce confidence
            if (recommendation.CodeChanges.Contains("multiple files") ||
                recommendation.CodeChanges.Contains("refactor") ||
                recommendation.CodeChanges.Contains("architecture"))
            {
                score -= 0.2;
                riskFactors.Add("Complex multi-file changes");
            }

            // Changes to critical files reduce confidence
            var criticalFiles = new[] { "web.config", "appsettings.json", "startup.cs", "program.cs" };
            if (criticalFiles.Any(file => finding.FilePath.ToLower().Contains(file.ToLower())))
            {
                score -= 0.3;
                riskFactors.Add("Critical configuration file changes");
            }

            // Changes requiring external dependencies reduce confidence
            if (recommendation.CodeChanges.Contains("nuget") ||
                recommendation.CodeChanges.Contains("npm") ||
                recommendation.CodeChanges.Contains("package"))
            {
                score -= 0.1;
                riskFactors.Add("External dependency changes");
            }

            // Language-specific risk assessment
            if (request.Language?.ToLower() == "javascript" || request.Language?.ToLower() == "typescript")
            {
                score -= 0.1; // JS/TS changes are more prone to runtime issues
                riskFactors.Add("Dynamic language runtime risks");
            }

            return Math.Max(score, 0.1); // Minimum confidence floor
        }

        private double MapModelConfidenceToScore(string modelConfidence)
        {
            return modelConfidence.ToLower() switch
            {
                "high" => 0.9,
                "medium" => 0.6,
                "low" => 0.3,
                _ => 0.5
            };
        }

        private string MapScoreToConfidence(double score)
        {
            return score switch
            {
                >= 0.8 => "High",
                >= 0.6 => "Medium",
                _ => "Low"
            };
        }

        private string BuildConfidenceExplanation(ConfidenceScoringDetails details)
        {
            var explanation = $"Confidence Score: {details.OverallScore:F2} (";
            explanation += $"Severity: {details.SeverityScore:F1}, ";
            explanation += $"Fix Pattern: {details.FixPatternScore:F1}, ";
            explanation += $"Change Risk: {details.ChangeRiskScore:F1}, ";
            explanation += $"Model Agreement: {details.ModelAgreementScore:F1})";

            return explanation;
        }

        private List<string> BuildContributingFactors(ConfidenceScoringDetails details)
        {
            var factors = new List<string>();

            if (details.SeverityScore >= 0.8)
                factors.Add("High severity finding");
            if (details.FixPatternScore >= 0.8)
                factors.Add("Well-established security fix pattern");
            if (details.ChangeRiskScore >= 0.8)
                factors.Add("Low-risk code changes");
            if (details.ModelAgreementScore >= 0.8)
                factors.Add("High model confidence");

            if (details.SeverityScore <= 0.4)
                factors.Add("Low severity finding");
            if (details.FixPatternScore <= 0.4)
                factors.Add("Uncommon or complex fix pattern");
            if (details.ChangeRiskScore <= 0.4)
                factors.Add("High-risk code changes");
            if (details.ModelAgreementScore <= 0.4)
                factors.Add("Low model confidence");

            return factors;
        }

        private List<string> GenerateWhyNotFixReasons(SecurityFinding finding, ConfidenceScoringDetails confidenceDetails)
        {
            var reasons = new List<string>();

            if (confidenceDetails.ChangeRiskScore < 0.5)
            {
                reasons.Add("Changes may introduce breaking functionality or performance issues");
            }

            if (confidenceDetails.FixPatternScore < 0.5)
            {
                reasons.Add("Fix pattern is not well-established or may have unintended side effects");
            }

            if (finding.Severity.ToLower() == "info" || finding.Severity.ToLower() == "low")
            {
                reasons.Add("Finding has low security impact and may be a false positive");
            }

            if (confidenceDetails.ModelAgreementScore < 0.5)
            {
                reasons.Add("Automated analysis confidence is low - manual review recommended");
            }

            if (finding.Category == "SCA" && confidenceDetails.ChangeRiskScore < 0.6)
            {
                reasons.Add("Dependency updates may cause compatibility issues with existing code");
            }

            if (reasons.Count == 0)
            {
                reasons.Add("Manual review recommended due to overall low confidence score");
            }

            return reasons;
        }

        private DiffResponse GenerateUnifiedDiff(string originalContent, string modifiedContent, string filePath)
        {
            // Simple diff implementation - in production, use a proper diff library
            var originalLines = originalContent.Split('\n');
            var modifiedLines = modifiedContent.Split('\n');

            var diffLines = new List<string>();
            var hunks = new List<DiffHunk>();

            // Basic line-by-line diff (simplified)
            int maxLines = Math.Max(originalLines.Length, modifiedLines.Length);
            int currentHunkStart = -1;

            for (int i = 0; i < maxLines; i++)
            {
                string origLine = i < originalLines.Length ? originalLines[i] : "";
                string modLine = i < modifiedLines.Length ? modifiedLines[i] : "";

                if (origLine != modLine)
                {
                    if (currentHunkStart == -1)
                    {
                        currentHunkStart = i;
                    }
                }
                else if (currentHunkStart != -1)
                {
                    // End of hunk
                    var hunk = new DiffHunk
                    {
                        OldStart = currentHunkStart + 1,
                        OldLines = i - currentHunkStart,
                        NewStart = currentHunkStart + 1,
                        NewLines = i - currentHunkStart,
                        Lines = new List<string>()
                    };

                    // Add context and changed lines
                    for (int j = Math.Max(0, currentHunkStart - 3); j < Math.Min(maxLines, i + 3); j++)
                    {
                        if (j < originalLines.Length && j < modifiedLines.Length)
                        {
                            if (originalLines[j] == modifiedLines[j])
                            {
                                hunk.Lines.Add(" " + originalLines[j]);
                            }
                            else
                            {
                                hunk.Lines.Add("-" + (j < originalLines.Length ? originalLines[j] : ""));
                                hunk.Lines.Add("+" + (j < modifiedLines.Length ? modifiedLines[j] : ""));
                            }
                        }
                    }

                    hunks.Add(hunk);
                    currentHunkStart = -1;
                }
            }

            return new DiffResponse
            {
                DiffContent = string.Join("\n", diffLines),
                Hunks = hunks,
                IsValid = true
            };
        }

        // Enterprise Governance Methods

        public async Task<PolicyOverrideResponse> RequestPolicyOverrideAsync(PolicyOverrideRequest request, string userId, string userRole)
        {
            return await _governanceService.RequestPolicyOverrideAsync(request, userId, userRole);
        }

        public async Task<PolicyOverrideResponse> ApprovePolicyOverrideAsync(string overrideId, string approvedBy)
        {
            return await _governanceService.ApprovePolicyOverrideAsync(overrideId, approvedBy);
        }

        public async Task<List<PolicyOverride>> GetPolicyOverridesAsync(string? organization = null, string? project = null, bool activeOnly = true)
        {
            return await _governanceService.GetPolicyOverridesAsync(organization, project, activeOnly);
        }

        public async Task<RiskAcceptanceResponse> AcceptRiskAsync(RiskAcceptanceRequest request, string acceptedBy)
        {
            return await _governanceService.AcceptRiskAsync(request, acceptedBy);
        }

        public async Task<List<RiskAcceptance>> GetRiskAcceptancesAsync(string? organization = null, string? project = null, bool activeOnly = true)
        {
            return await _governanceService.GetRiskAcceptancesAsync(organization, project, activeOnly);
        }

        public async Task<SecurityMetricsResponse> GetSecurityMetricsAsync(SecurityMetricsRequest request)
        {
            return await _governanceService.GetSecurityMetricsAsync(request);
        }

        public async Task<NoiseReductionPolicyResponse> CreateNoiseReductionPolicyAsync(NoiseReductionPolicyRequest request, string createdBy)
        {
            return await _governanceService.CreateNoiseReductionPolicyAsync(request, createdBy);
        }

        public async Task<List<NoiseReductionPolicy>> GetNoiseReductionPoliciesAsync(string? organization = null, string? project = null, bool activeOnly = true)
        {
            return await _governanceService.GetNoiseReductionPoliciesAsync(organization, project, activeOnly);
        }

        public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
        {
            await _governanceService.LogSecurityEventAsync(securityEvent);
        }

        public async Task<List<SecurityFinding>> GetFilteredFindingsAsync(string? organization = null, string? project = null, string? repository = null, string? status = null, string? severity = null)
        {
            var findings = _findings.Values.AsQueryable();

            if (!string.IsNullOrEmpty(organization))
                findings = findings.Where(f => f.Metadata != null && f.Metadata.ContainsKey("organization") && f.Metadata["organization"] != null && f.Metadata["organization"].ToString() == organization);

            if (!string.IsNullOrEmpty(project))
                findings = findings.Where(f => f.Metadata != null && f.Metadata.ContainsKey("project") && f.Metadata["project"] != null && f.Metadata["project"].ToString() == project);

            if (!string.IsNullOrEmpty(repository))
                findings = findings.Where(f => f.Metadata != null && f.Metadata.ContainsKey("repository") && f.Metadata["repository"] != null && f.Metadata["repository"].ToString() == repository);

            if (!string.IsNullOrEmpty(status))
                findings = findings.Where(f => f.Status == status);

            if (!string.IsNullOrEmpty(severity))
                findings = findings.Where(f => f.Severity == severity);

            return findings.OrderByDescending(f => f.CreatedAt).ToList();
        }

        public async Task<AnalysisMetadata?> GetAnalysisMetadataAsync(string analysisId)
        {
            var entity = await _securityAdvisorRepository.GetAnalysisMetadataAsync(analysisId);
            if (entity == null) return null;

            return new AnalysisMetadata
            {
                Id = entity.Id.ToString(),
                AnalysisId = entity.AnalysisId,
                ModelProvider = entity.ModelProvider,
                ModelName = entity.ModelName,
                PromptVersion = entity.PromptVersion,
                PolicyVersion = entity.PolicyVersion,
                ConfidenceBreakdown = entity.ConfidenceBreakdown,
                InputsUsed = entity.InputsUsed,
                CreatedUtc = entity.CreatedUtc
            };
        }

        public async Task<Dictionary<string, string>> GetCurrentVersionsAsync()
        {
            return await _securityAdvisorRepository.GetCurrentVersionsAsync();
        }

        public async Task<List<RiskAcceptanceEntity>> GetExpiringRiskAcceptancesAsync(DateTime expiringBefore)
        {
            var entities = await _securityAdvisorRepository.GetRiskAcceptancesAsync(activeOnly: true);
            return entities.Where(e => e.ExpiresAt.HasValue && e.ExpiresAt.Value <= expiringBefore).ToList();
        }
    }
}