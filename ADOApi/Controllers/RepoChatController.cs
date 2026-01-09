using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ADOApi.Models.Chat;
using ADOApi.Services.Chat;
using ADOApi.Services;
using System.Security.Claims;

namespace ADOApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/chat")]
    [Authorize(Policy = "ADO.ReadOnly")]
    public class RepoChatController : ControllerBase
    {
        private readonly RepoChatAgentService _chatAgentService;
        private readonly RepoChatContextBuilder _contextBuilder;
        private readonly WorkItemProposalService _proposalService;

        public RepoChatController(
            RepoChatAgentService chatAgentService,
            RepoChatContextBuilder contextBuilder,
            WorkItemProposalService proposalService)
        {
            _chatAgentService = chatAgentService;
            _contextBuilder = contextBuilder;
            _proposalService = proposalService;
        }

        [HttpPost("repo")]
        [ProducesResponseType(typeof(RepoChatResponse), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<RepoChatResponse>> ChatWithRepository([FromBody] RepoChatRequest request)
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check authorization based on mode
            if (!IsAuthorizedForMode(request.Mode))
            {
                return Forbid();
            }

            try
            {
                // Build context
                var context = await _contextBuilder.BuildContextAsync(request);

                // Generate chat message ID for tracking
                var chatMessageId = Guid.NewGuid();

                // Run the chat agent
                var response = await _chatAgentService.RunChatAsync(request, context);

                // If mode is Plan and proposals exist, persist them
                if (request.Mode == "Plan" && response.Proposals.Any())
                {
                    await _proposalService.AddChatProposalsAsync(
                        request.RepoKey,
                        chatMessageId,
                        response.Proposals);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Return error response
                return Ok(new RepoChatResponse
                {
                    Reply = "An error occurred while processing your request. Please try again.",
                    Confidence = 0.0,
                    Sources = new List<ChatSource>(),
                    Proposals = new List<WorkItemProposalDraft>(),
                    Notes = $"Error: {ex.Message}"
                });
            }
        }

        private bool IsAuthorizedForMode(string mode)
        {
            // Check if user has required role for the mode
            var user = HttpContext.User;

            switch (mode)
            {
                case "Explore":
                case "Review":
                    // ReadOnly+ access (ADO.ReadOnly, ADO.Contributor, ADO.Admin)
                    return user.IsInRole("ADO.ReadOnly") ||
                           user.IsInRole("ADO.Contributor") ||
                           user.IsInRole("ADO.Admin");

                case "Plan":
                case "MemoryDraft":
                    // Contributor+ access (ADO.Contributor, ADO.Admin)
                    return user.IsInRole("ADO.Contributor") ||
                           user.IsInRole("ADO.Admin");

                default:
                    return false;
            }
        }
    }
}