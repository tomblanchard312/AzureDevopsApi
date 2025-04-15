using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using ADOApi.Interfaces;
using ADOApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SemanticKernelController : ControllerBase
    {
        private readonly IWorkItemService _workItemService;
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatService;

        // Fix for IDE0290: Use primary constructor
        public SemanticKernelController(IWorkItemService workItemService, Kernel kernel)
        {
            _workItemService = workItemService;
            _kernel = kernel;
            _chatService = kernel.GetRequiredService<IChatCompletionService>();
        }

        [HttpPost("query")]
        public async Task<ActionResult<string>> QueryWorkItems([FromBody] WorkItemFilterRequest request)
        {
            try
            {
                // Get relevant work items based on the question
                var workItems = await _workItemService.QueryByWiqlAsync(new Wiql
                {
                    Query = $"SELECT [System.Id], [System.Title], [System.Description] FROM WorkItems WHERE [System.TeamProject] = '{request.Project}'" +
                           (string.IsNullOrEmpty(request.TitleContains) ? "" : $" AND [System.Title] CONTAINS '{request.TitleContains}'") +
                           (string.IsNullOrEmpty(request.DescriptionContains) ? "" : $" AND [System.Description] CONTAINS '{request.DescriptionContains}'")
                }, request.Project);

                // Create a prompt with the work items
                var prompt = $"Based on the following work items, answer the question: {request.TitleContains}\n\n";
                foreach (var workItem in workItems.WorkItems)
                {
                    var details = await _workItemService.GetWorkItemAsync(workItem.Id);
                    prompt += $"Work Item {details.Id}: {details.Fields["System.Title"]}\n";
                    if (details.Fields.ContainsKey("System.Description"))
                    {
                        prompt += $"Description: {details.Fields["System.Description"]}\n";
                    }
                    prompt += "\n";
                }

                // Create a chat history with the prompt
                var history = new ChatHistory();
                history.AddUserMessage(prompt);

                // Get the answer from the chat completion service
                var result = await _chatService.GetChatMessageContentAsync(history);
                return Ok(result.Content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
} 