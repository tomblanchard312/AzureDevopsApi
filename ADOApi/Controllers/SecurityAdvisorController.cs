using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace ADOApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Policy = "ADO.ReadOnly")]
    public class SecurityAdvisorController : ControllerBase
    {
        private readonly ISecurityAdvisorService _securityAdvisorService;
        private readonly ILogger<SecurityAdvisorController> _logger;
        private readonly IAuditLogger _auditLogger;

        public SecurityAdvisorController(
            ISecurityAdvisorService securityAdvisorService,
            ILogger<SecurityAdvisorController> logger,
            IAuditLogger auditLogger)
        {
            _securityAdvisorService = securityAdvisorService;
            _logger = logger;
            _auditLogger = auditLogger;
        }

        [HttpPost("analyze/sarif")]
        [ProducesResponseType(typeof(SecurityAnalysisResponse), 200)]
        public async Task<ActionResult<SecurityAnalysisResponse>> AnalyzeSarif([FromBody] SarifAnalysisRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

            var auditEvent = new Models.AuditEvent
            {
                Action = "AnalyzeSarif",
                TargetType = "security",
                TargetId = correlationId,
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var result = await _securityAdvisorService.AnalyzeSarifAsync(request);
                auditEvent.Success = true;
                await _auditLogger.AuditAsync(auditEvent);
                return Ok(result);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error analyzing SARIF for correlation {CorrelationId}", correlationId);
                return StatusCode(500, "An error occurred during analysis");
            }
        }

        [HttpPost("analyze/sbom")]
        [ProducesResponseType(typeof(SecurityAnalysisResponse), 200)]
        public async Task<ActionResult<SecurityAnalysisResponse>> AnalyzeSbom([FromBody] SbomAnalysisRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

            var auditEvent = new Models.AuditEvent
            {
                Action = "AnalyzeSbom",
                TargetType = "security",
                TargetId = correlationId,
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var result = await _securityAdvisorService.AnalyzeSbomAsync(request);
                auditEvent.Success = true;
                await _auditLogger.AuditAsync(auditEvent);
                return Ok(result);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error analyzing SBOM for correlation {CorrelationId}", correlationId);
                return StatusCode(500, "An error occurred during analysis");
            }
        }

        [HttpPost("recommendations/{findingId}")]
        [ProducesResponseType(typeof(SecurityRecommendation), 200)]
        public async Task<ActionResult<SecurityRecommendation>> GetRecommendation(string findingId, [FromBody] RecommendationRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

            var auditEvent = new Models.AuditEvent
            {
                Action = "GetRecommendation",
                TargetType = "security",
                TargetId = findingId,
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var result = await _securityAdvisorService.GenerateRecommendationAsync(findingId, request);
                auditEvent.Success = true;
                await _auditLogger.AuditAsync(auditEvent);
                return Ok(result);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error generating recommendation for finding {FindingId}", findingId);
                return StatusCode(500, "An error occurred generating recommendation");
            }
        }

        [HttpPost("diff/{recommendationId}")]
        [ProducesResponseType(typeof(DiffResponse), 200)]
        public async Task<ActionResult<DiffResponse>> GenerateDiff(string recommendationId, [FromBody] DiffRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

            var auditEvent = new Models.AuditEvent
            {
                Action = "GenerateDiff",
                TargetType = "security",
                TargetId = recommendationId,
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var result = await _securityAdvisorService.GenerateDiffAsync(recommendationId, request);
                auditEvent.Success = true;
                await _auditLogger.AuditAsync(auditEvent);
                return Ok(result);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error generating diff for recommendation {RecommendationId}", recommendationId);
                return StatusCode(500, "An error occurred generating diff");
            }
        }

        [HttpPost("apply/{recommendationId}")]
        [Authorize(Policy = "ADO.Contributor")]
        [ProducesResponseType(typeof(ApplyResponse), 200)]
        public async Task<ActionResult<ApplyResponse>> ApplyRecommendation(string recommendationId, [FromBody] ApplyRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

            var auditEvent = new Models.AuditEvent
            {
                Action = "ApplyRecommendation",
                TargetType = "security",
                TargetId = recommendationId,
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var result = await _securityAdvisorService.ApplyRecommendationAsync(recommendationId, request);
                auditEvent.Success = true;
                await _auditLogger.AuditAsync(auditEvent);
                return Ok(result);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error applying recommendation {RecommendationId}", recommendationId);
                return StatusCode(500, "An error occurred applying recommendation");
            }
        }

        [HttpGet("findings")]
        [ProducesResponseType(typeof(List<SecurityFinding>), 200)]
        public async Task<ActionResult<List<SecurityFinding>>> GetFindings([FromQuery] string? status = null)
        {
            try
            {
                var findings = await _securityAdvisorService.GetFindingsAsync(status);
                return Ok(findings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security findings");
                return StatusCode(500, "An error occurred retrieving findings");
            }
        }

        [HttpPost("pull-request/comment")]
        [Authorize(Policy = "ADO.Contributor")]
        [ProducesResponseType(typeof(PullRequestCommentResponse), 200)]
        public async Task<ActionResult<PullRequestCommentResponse>> PostPullRequestComment([FromBody] PullRequestCommentRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

            var auditEvent = new Models.AuditEvent
            {
                Action = request.PreviewOnly ? "PreviewPullRequestComment" : "PostPullRequestComment",
                TargetType = "pull-request",
                TargetId = $"{request.Project}/{request.RepositoryId}/PR-{request.PullRequestId}",
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var prCommentService = HttpContext.RequestServices.GetRequiredService<IPullRequestCommentService>();
                var response = await prCommentService.GenerateAndPostCommentAsync(request);

                auditEvent.Success = response.Success;
                if (!response.Success)
                {
                    auditEvent.ErrorMessage = SanitizeForLog(response.ErrorMessage ?? "Unknown error");
                }

                await _auditLogger.AuditAsync(auditEvent);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error posting PR comment for PR {PRId}", request.PullRequestId);
                return StatusCode(500, "An error occurred posting PR comment");
            }
        }

        [HttpPost("pull-request/comment/preview")]
        [Authorize(Policy = "ADO.ReadOnly")]
        [ProducesResponseType(typeof(PullRequestCommentResponse), 200)]
        public async Task<ActionResult<PullRequestCommentResponse>> PreviewPullRequestComment([FromBody] PullRequestCommentRequest request)
        {
            try
            {
                var prCommentService = HttpContext.RequestServices.GetRequiredService<IPullRequestCommentService>();
                var response = await prCommentService.PreviewCommentAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing PR comment for PR {PRId}", request.PullRequestId);
                return StatusCode(500, "An error occurred previewing PR comment");
            }
        }

        [HttpPut("pull-request/comment/{threadId}")]
        [Authorize(Policy = "ADO.Contributor")]
        [ProducesResponseType(typeof(PullRequestCommentResponse), 200)]
        public async Task<ActionResult<PullRequestCommentResponse>> UpdatePullRequestComment(int threadId, [FromBody] PullRequestCommentRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

            var auditEvent = new Models.AuditEvent
            {
                Action = "UpdatePullRequestComment",
                TargetType = "pull-request-thread",
                TargetId = $"{request.Project}/{request.RepositoryId}/PR-{request.PullRequestId}/Thread-{threadId}",
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var prCommentService = HttpContext.RequestServices.GetRequiredService<IPullRequestCommentService>();
                var response = await prCommentService.UpdateCommentAsync(threadId, request);

                auditEvent.Success = response.Success;
                if (!response.Success)
                {
                    auditEvent.ErrorMessage = SanitizeForLog(response.ErrorMessage ?? "Unknown error");
                }

                await _auditLogger.AuditAsync(auditEvent);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error updating PR comment thread {ThreadId}", threadId);
                return StatusCode(500, "An error occurred updating PR comment");
            }
        }

        [HttpPost("pr/inline-comment")]
        [Authorize(Policy = "ADO.Contributor")]
        [ProducesResponseType(typeof(InlineCommentResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<InlineCommentResponse>> PostInlineComment([FromBody] InlineCommentRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

            var auditEvent = new Models.AuditEvent
            {
                Action = "PostInlineComment",
                TargetType = "pull-request-inline",
                TargetId = $"{request.Project}/{request.RepositoryId}/PR-{request.PullRequestId}/Finding-{request.FindingId}",
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var prCommentService = HttpContext.RequestServices.GetRequiredService<IPullRequestCommentService>();
                var response = await prCommentService.PostInlineCommentAsync(request);

                auditEvent.Success = response.Success;
                if (!response.Success)
                {
                    auditEvent.ErrorMessage = SanitizeForLog(response.ErrorMessage ?? "Unknown error");
                }

                await _auditLogger.AuditAsync(auditEvent);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error posting inline comment for finding {FindingId}", request.FindingId);
                return StatusCode(500, "An error occurred posting inline comment");
            }
        }

        [HttpPost("pr/resolve-threads")]
        [Authorize(Policy = "ADO.Contributor")]
        [ProducesResponseType(typeof(ThreadResolutionResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ThreadResolutionResponse>> ResolveFixedThreads([FromBody] ThreadResolutionRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

            var auditEvent = new Models.AuditEvent
            {
                Action = "ResolveFixedThreads",
                TargetType = "pull-request-threads",
                TargetId = $"{request.Project}/{request.RepositoryId}/PR-{request.PullRequestId}",
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var prCommentService = HttpContext.RequestServices.GetRequiredService<IPullRequestCommentService>();
                var response = await prCommentService.ResolveFixedThreadsAsync(request);

                auditEvent.Success = response.Success;
                if (!response.Success)
                {
                    auditEvent.ErrorMessage = SanitizeForLog(response.ErrorMessage ?? "Unknown error");
                }

                await _auditLogger.AuditAsync(auditEvent);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error resolving fixed threads for PR {PRId}", request.PullRequestId);
                return StatusCode(500, "An error occurred resolving threads");
            }
        }

        [HttpPost("pr/status")]
        [Authorize(Policy = "ADO.Contributor")]
        [ProducesResponseType(typeof(PrStatusResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PrStatusResponse>> PostPrStatus([FromBody] PrStatusRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

            var auditEvent = new Models.AuditEvent
            {
                Action = "PostPrStatus",
                TargetType = "pull-request-status",
                TargetId = $"{request.Project}/{request.RepositoryId}/PR-{request.PullRequestId}",
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var prCommentService = HttpContext.RequestServices.GetRequiredService<IPullRequestCommentService>();
                var response = await prCommentService.PostPrStatusAsync(request);

                auditEvent.Success = response.Success;
                if (!response.Success)
                {
                    auditEvent.ErrorMessage = SanitizeForLog(response.ErrorMessage ?? "Unknown error");
                }

                await _auditLogger.AuditAsync(auditEvent);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error posting PR status for PR {PRId}", request.PullRequestId);
                return StatusCode(500, "An error occurred posting PR status");
            }
        }

        // Enterprise Governance Endpoints

        [HttpPost("governance/override")]
        [Authorize(Policy = "ADO.Contributor")]
        [ProducesResponseType(typeof(PolicyOverrideResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PolicyOverrideResponse>> RequestPolicyOverride([FromBody] PolicyOverrideRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();
            var userId = HttpContext.Items["ActorUpn"] as string ?? "unknown";
            var userRole = GetUserRole();

            var auditEvent = new Models.AuditEvent
            {
                Action = "RequestPolicyOverride",
                TargetType = "policy-override",
                TargetId = request.FindingId,
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = userId
            };

            try
            {
                var response = await _securityAdvisorService.RequestPolicyOverrideAsync(request, userId, userRole);

                auditEvent.Success = response.Success;
                if (!response.Success)
                {
                    auditEvent.ErrorMessage = SanitizeForLog(response.ErrorMessage ?? "Unknown error");
                }

                await _auditLogger.AuditAsync(auditEvent);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error requesting policy override for finding {FindingId}", request.FindingId);
                return StatusCode(500, "An error occurred requesting policy override");
            }
        }

        [HttpPost("governance/override/{overrideId}/approve")]
        [Authorize(Policy = "ADO.Admin")]
        [ProducesResponseType(typeof(PolicyOverrideResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PolicyOverrideResponse>> ApprovePolicyOverride(string overrideId)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();
            var approvedBy = HttpContext.Items["ActorUpn"] as string ?? "unknown";

            var auditEvent = new Models.AuditEvent
            {
                Action = "ApprovePolicyOverride",
                TargetType = "policy-override",
                TargetId = overrideId,
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = approvedBy
            };

            try
            {
                var response = await _securityAdvisorService.ApprovePolicyOverrideAsync(overrideId, approvedBy);

                auditEvent.Success = response.Success;
                if (!response.Success)
                {
                    auditEvent.ErrorMessage = SanitizeForLog(response.ErrorMessage ?? "Unknown error");
                }

                await _auditLogger.AuditAsync(auditEvent);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error approving policy override {OverrideId}", overrideId);
                return StatusCode(500, "An error occurred approving policy override");
            }
        }

        [HttpGet("governance/overrides")]
        [Authorize(Policy = "ADO.ReadOnly")]
        [ProducesResponseType(typeof(List<PolicyOverride>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<PolicyOverride>>> GetPolicyOverrides(
            [FromQuery] string? organization = null,
            [FromQuery] string? project = null,
            [FromQuery] bool activeOnly = true)
        {
            try
            {
                var overrides = await _securityAdvisorService.GetPolicyOverridesAsync(organization, project, activeOnly);
                return Ok(overrides);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving policy overrides");
                return StatusCode(500, "An error occurred retrieving policy overrides");
            }
        }

        [HttpPost("governance/risk-acceptance")]
        [Authorize(Policy = "ADO.Contributor")]
        [ProducesResponseType(typeof(RiskAcceptanceResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<RiskAcceptanceResponse>> AcceptRisk([FromBody] RiskAcceptanceRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();
            var acceptedBy = HttpContext.Items["ActorUpn"] as string ?? "unknown";

            var auditEvent = new Models.AuditEvent
            {
                Action = "AcceptRisk",
                TargetType = "risk-acceptance",
                TargetId = request.FindingId,
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = acceptedBy
            };

            try
            {
                var response = await _securityAdvisorService.AcceptRiskAsync(request, acceptedBy);

                auditEvent.Success = response.Success;
                if (!response.Success)
                {
                    auditEvent.ErrorMessage = SanitizeForLog(response.ErrorMessage ?? "Unknown error");
                }

                await _auditLogger.AuditAsync(auditEvent);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error accepting risk for finding {FindingId}", request.FindingId);
                return StatusCode(500, "An error occurred accepting risk");
            }
        }

        [HttpGet("governance/risk-acceptances")]
        [Authorize(Policy = "ADO.ReadOnly")]
        [ProducesResponseType(typeof(List<RiskAcceptance>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<RiskAcceptance>>> GetRiskAcceptances(
            [FromQuery] string? organization = null,
            [FromQuery] string? project = null,
            [FromQuery] bool activeOnly = true)
        {
            try
            {
                var acceptances = await _securityAdvisorService.GetRiskAcceptancesAsync(organization, project, activeOnly);
                return Ok(acceptances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving risk acceptances");
                return StatusCode(500, "An error occurred retrieving risk acceptances");
            }
        }

        [HttpGet("governance/metrics")]
        [Authorize(Policy = "ADO.ReadOnly")]
        [ProducesResponseType(typeof(SecurityMetricsResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SecurityMetricsResponse>> GetSecurityMetrics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? organization = null,
            [FromQuery] string? project = null)
        {
            try
            {
                var request = new SecurityMetricsRequest
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Organization = organization,
                    Project = project
                };

                var response = await _securityAdvisorService.GetSecurityMetricsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security metrics");
                return StatusCode(500, "An error occurred retrieving security metrics");
            }
        }

        [HttpPost("governance/noise-policy")]
        [Authorize(Policy = "ADO.Admin")]
        [ProducesResponseType(typeof(NoiseReductionPolicyResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<NoiseReductionPolicyResponse>> CreateNoiseReductionPolicy([FromBody] NoiseReductionPolicyRequest request)
        {
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();
            var createdBy = HttpContext.Items["ActorUpn"] as string ?? "unknown";

            var auditEvent = new Models.AuditEvent
            {
                Action = "CreateNoiseReductionPolicy",
                TargetType = "noise-policy",
                TargetId = request.RuleId,
                CorrelationId = correlationId,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = createdBy
            };

            try
            {
                var response = await _securityAdvisorService.CreateNoiseReductionPolicyAsync(request, createdBy);

                auditEvent.Success = response.Success;
                if (!response.Success)
                {
                    auditEvent.ErrorMessage = SanitizeForLog(response.ErrorMessage ?? "Unknown error");
                }

                await _auditLogger.AuditAsync(auditEvent);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);
                _logger.LogError(ex, "Error creating noise reduction policy for rule {RuleId}", request.RuleId);
                return StatusCode(500, "An error occurred creating noise reduction policy");
            }
        }

        [HttpGet("governance/noise-policies")]
        [Authorize(Policy = "ADO.ReadOnly")]
        [ProducesResponseType(typeof(List<NoiseReductionPolicy>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<NoiseReductionPolicy>>> GetNoiseReductionPolicies(
            [FromQuery] string? organization = null,
            [FromQuery] string? project = null,
            [FromQuery] bool activeOnly = true)
        {
            try
            {
                var policies = await _securityAdvisorService.GetNoiseReductionPoliciesAsync(organization, project, activeOnly);
                return Ok(policies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving noise reduction policies");
                return StatusCode(500, "An error occurred retrieving noise reduction policies");
            }
        }

        [HttpGet("findings/filtered")]
        [Authorize(Policy = "ADO.ReadOnly")]
        [ProducesResponseType(typeof(List<SecurityFinding>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<SecurityFinding>>> GetFilteredFindings(
            [FromQuery] string? organization = null,
            [FromQuery] string? project = null,
            [FromQuery] string? repository = null,
            [FromQuery] string? status = null,
            [FromQuery] string? severity = null)
        {
            try
            {
                var findings = await _securityAdvisorService.GetFilteredFindingsAsync(organization, project, repository, status, severity);
                return Ok(findings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filtered findings");
                return StatusCode(500, "An error occurred retrieving filtered findings");
            }
        }

        [HttpGet("analysis/{analysisId}/metadata")]
        [ProducesResponseType(typeof(AnalysisMetadataResponse), 200)]
        public async Task<ActionResult<AnalysisMetadataResponse>> GetAnalysisMetadata(string analysisId)
        {
            try
            {
                var metadata = await _securityAdvisorService.GetAnalysisMetadataAsync(analysisId);
                if (metadata == null)
                {
                    return NotFound(new AnalysisMetadataResponse
                    {
                        Success = false,
                        ErrorMessage = "Analysis metadata not found"
                    });
                }

                return Ok(new AnalysisMetadataResponse
                {
                    Success = true,
                    Metadata = new AnalysisMetadata
                    {
                        Id = metadata.Id.ToString(),
                        AnalysisId = metadata.AnalysisId,
                        ModelProvider = metadata.ModelProvider,
                        ModelName = metadata.ModelName,
                        PromptVersion = metadata.PromptVersion,
                        PolicyVersion = metadata.PolicyVersion,
                        ConfidenceBreakdown = metadata.ConfidenceBreakdown,
                        InputsUsed = metadata.InputsUsed,
                        CreatedUtc = metadata.CreatedUtc
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analysis metadata for {AnalysisId}", analysisId);
                return StatusCode(500, new AnalysisMetadataResponse
                {
                    Success = false,
                    ErrorMessage = "An error occurred retrieving analysis metadata"
                });
            }
        }

        [HttpGet("config/versions")]
        [ProducesResponseType(typeof(VersionInfoResponse), 200)]
        public async Task<ActionResult<VersionInfoResponse>> GetVersions()
        {
            try
            {
                var versions = await _securityAdvisorService.GetCurrentVersionsAsync();
                return Ok(new VersionInfoResponse
                {
                    Success = true,
                    Versions = new VersionInfo
                    {
                        PromptVersion = versions["PromptVersion"],
                        PolicyVersion = versions["PolicyVersion"]
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving version information");
                return StatusCode(500, new VersionInfoResponse
                {
                    Success = false,
                    ErrorMessage = "An error occurred retrieving version information"
                });
            }
        }

        [HttpPost("analysis/{prId}/rerun")]
        [Authorize(Policy = "ADO.Contributor")]
        [ProducesResponseType(typeof(ReAnalysisResponse), 200)]
        public async Task<ActionResult<ReAnalysisResponse>> RerunAnalysis(string prId, [FromBody] ReAnalysisRequest? request = null)
        {
            var userId = HttpContext.Items["ActorUpn"] as string ?? "unknown";
            var userRole = GetUserRole();

            var auditEvent = new Models.AuditEvent
            {
                Action = "RerunAnalysis",
                TargetType = "pull-request",
                TargetId = prId,
                CorrelationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString(),
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = userId
            };

            try
            {
                // TODO: Implement actual re-analysis logic
                // For now, just log the event and return success
                await _securityAdvisorService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = "analysis_rerun",
                    Organization = request?.Organization,
                    Project = request?.Project,
                    Repository = request?.Repository,
                    UserId = userId,
                    UserRole = userRole,
                    Timestamp = DateTime.UtcNow,
                    Properties = new Dictionary<string, object>
                    {
                        ["prId"] = prId,
                        ["commitSha"] = request?.CommitSha ?? "latest"
                    }
                });

                auditEvent.Success = true;
                await _auditLogger.AuditAsync(auditEvent);

                return Ok(new ReAnalysisResponse
                {
                    Success = true,
                    AnalysisId = Guid.NewGuid().ToString(),
                    StartedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-running analysis for PR {PrId}", prId);
                auditEvent.Success = false;
                auditEvent.ErrorMessage = SanitizeForLog(ex.Message);
                await _auditLogger.AuditAsync(auditEvent);

                return StatusCode(500, new ReAnalysisResponse
                {
                    Success = false,
                    ErrorMessage = "An error occurred re-running analysis"
                });
            }
        }

        [HttpGet("risk-acceptance/expiring")]
        [ProducesResponseType(typeof(ExpiringRiskAcceptancesResponse), 200)]
        public async Task<ActionResult<ExpiringRiskAcceptancesResponse>> GetExpiringRiskAcceptances([FromQuery] int daysAhead = 14)
        {
            try
            {
                var expiringDate = DateTime.UtcNow.AddDays(daysAhead);
                var acceptances = await _securityAdvisorService.GetExpiringRiskAcceptancesAsync(expiringDate);

                var expiringAcceptances = acceptances.Select(a => new ExpiringRiskAcceptance
                {
                    Id = a.Id.ToString(),
                    FindingId = a.FindingId,
                    Organization = a.Organization,
                    Project = a.Project,
                    Repository = a.Repository,
                    AcceptedBy = a.AcceptedBy,
                    AcceptedAt = a.AcceptedAt,
                    ExpiresAt = a.ExpiresAt ?? DateTime.MaxValue,
                    DaysUntilExpiry = a.ExpiresAt.HasValue ? (int)(a.ExpiresAt.Value - DateTime.UtcNow).TotalDays : int.MaxValue
                }).ToList();

                return Ok(new ExpiringRiskAcceptancesResponse
                {
                    Success = true,
                    ExpiringAcceptances = expiringAcceptances
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expiring risk acceptances");
                return StatusCode(500, new ExpiringRiskAcceptancesResponse
                {
                    Success = false,
                    ErrorMessage = "An error occurred retrieving expiring risk acceptances"
                });
            }
        }

        private string GetUserRole()
        {
            // This is a simplified role detection - in production, this would check claims or roles from Entra ID
            // For now, we'll assume the authorization policies handle the role checking
            if (User.IsInRole("ADO.Admin")) return "Admin";
            if (User.IsInRole("ADO.Contributor")) return "Contributor";
            return "ReadOnly";
        }

        private static string SanitizeForLog(string input) => input?.Replace("\n", "").Replace("\r", "").Replace("\t", "") ?? "";
    }
}