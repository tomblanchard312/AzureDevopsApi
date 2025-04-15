using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ADOApi.Models;
using ADOApi.Services;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using ADOApi.Exceptions;

namespace ADOApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class WorkItemController : ControllerBase
    {
        private readonly AzureDevOpsService _azureDevOpsService;
        private readonly ILogger<WorkItemController> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public WorkItemController(AzureDevOpsService azureDevOpsService, ILogger<WorkItemController> logger)
        {
            _azureDevOpsService = azureDevOpsService;
            _logger = logger;
            
            // Configure retry policy
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, 
                            "Retry {RetryCount} after {Delay}ms due to: {Message}", 
                            retryCount, timeSpan.TotalMilliseconds, exception.Message);
                    });
        }

        [HttpGet("workitemtypes")]
        public async Task<ActionResult<List<string>>> GetWorkItemTypes(string project)
        {
            try
            {
                var workItemTypes = await _azureDevOpsService.GetWorkItemTypesAsync(project);
                return Ok(workItemTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpGet("workitemforproject")]
        public async Task<ActionResult<List<WorkItem>>> GetAllWorkItemsForProjectAsync(string project) {
            try
            {
                List<WorkItem> workItems = await _azureDevOpsService.GetAllWorkItemsForProjectAsync(project);
                return Ok(workItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpGet("workitemsassignedtouser")]
        public async Task<ActionResult<List<WorkItem>>> GetMyAssignedWorkItemsAsync(string project, string UserIdentifier)
        {
            try
            {
                List<WorkItem> workItems = await _azureDevOpsService.GetMyAssignedWorkItemsAsync(project, UserIdentifier);
                return Ok(workItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }        
        [HttpGet("workitemsbytype")]
        public async Task<ActionResult<List<WorkItem>>> GetWorkItemsByType(string project, string workItemType)
        {
            try
            {
                var workItems = await _azureDevOpsService.GetWorkItemsByTypeAsync(project, workItemType);
                return Ok(workItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<ActionResult<int>> AddWorkItem([FromBody] WorkItemDetailsRequest request)
        {
            if (string.IsNullOrEmpty(request.Project) || string.IsNullOrEmpty(request.WorkItemType))
            {
                return BadRequest("Project and WorkItemType are required");
            }

            try
            {
                var workItemId = await _azureDevOpsService.AddWorkItemAsync(
                    request.Project,
                    request.WorkItemType,
                    request.Title ?? string.Empty,
                    request.Description ?? string.Empty,
                    request.AssignedTo ?? string.Empty,
                    request.Tag ?? string.Empty,
                    request.EffortHours,
                    request.Comments ?? string.Empty,
                    request.ParentWorkItemId);

                return Ok(workItemId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding work item: {ex.Message}");
            }
        }
        [HttpPut("{workItemId}")]
        public async Task<ActionResult<bool>> UpdateWorkItem(
            int workItemId,
            [FromBody] WorkItemUpdateRequest request)
        {
            if (string.IsNullOrEmpty(request.State))
            {
                return BadRequest("State is required");
            }

            try
            {
                var result = await _azureDevOpsService.UpdateWorkItemAsync(
                    workItemId,
                    request.State,
                    request.Comment ?? string.Empty,
                    request.AssignedUser ?? string.Empty,
                    request.Priority,
                    request.RemainingEffortHours,
                    request.CompletedEffortHours,
                    request.Tag);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating work item: {ex.Message}");
            }
        }
        [HttpGet("workitembyid")]
        public async Task<IActionResult> GetWorkItemById(int workItemId, string project)
        {
            try
            {

                WorkItem workItem = await _azureDevOpsService.GetWorkItemByIdAsync(workItemId, project);

                if (workItem == null)
                {
                    return NotFound();
                }

                return Ok(workItem);
            }
            catch (AzureDevOpsApiException ex)
            {
                // Handle the exception and return an appropriate error response
                return StatusCode(500, $"An error occurred while retrieving the work item: {ex.Message}");
            }
        }
        [HttpGet("recent")]
        public async Task<ActionResult<List<RecentWorkItem>>> GetRecentWorkItems()
        {
            try
            {
                var workItems = await _azureDevOpsService.GetRecentWorkItemsAsync();
                return Ok(workItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving recent work items: {ex.Message}");
            }
        }
        [HttpGet("{workItemId}")]
        public async Task<ActionResult<WorkItem>> GetWorkItem(int workItemId, [FromQuery] string project)
        {
            if (string.IsNullOrEmpty(project))
            {
                return BadRequest("Project is required");
            }

            try
            {
                var workItem = await _azureDevOpsService.GetWorkItemByIdAsync(workItemId, project);
                return Ok(workItem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving work item: {ex.Message}");
            }
        }
        [HttpGet("project/{project}")]
        public async Task<ActionResult<List<WorkItem>>> GetAllWorkItemsForProject(string project)
        {
            try
            {
                var workItems = await _azureDevOpsService.GetAllWorkItemsForProjectAsync(project);
                return Ok(workItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving project work items: {ex.Message}");
            }
        }
        [HttpGet("assigned/{project}/{userIdentifier}")]
        public async Task<ActionResult<List<WorkItem>>> GetMyAssignedWorkItems(string project, string userIdentifier)
        {
            try
            {
                var workItems = await _azureDevOpsService.GetMyAssignedWorkItemsAsync(project, userIdentifier);
                return Ok(workItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving assigned work items: {ex.Message}");
            }
        }

        [HttpPost("templates")]
        public async Task<ActionResult<int>> CreateTemplate([FromBody] ADOApi.Models.WorkItemTemplate template)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var templateId = await _azureDevOpsService.CreateWorkItemTemplateAsync(template);
                    return Ok(templateId);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work item template");
                return StatusCode(500, new { error = "Failed to create template", details = ex.Message });
            }
        }

        [HttpGet("templates/{project}")]
        public async Task<ActionResult<List<ADOApi.Models.WorkItemTemplate>>> GetTemplates(string project)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var templates = await _azureDevOpsService.GetWorkItemTemplatesAsync(project);
                    return Ok(templates);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving work item templates");
                return StatusCode(500, new { error = "Failed to retrieve templates", details = ex.Message });
            }
        }

        [HttpPost("from-template/{templateId}")]
        public async Task<ActionResult<int>> CreateFromTemplate(int templateId, [FromBody] Dictionary<string, object>? overrides = null)
        {
            try
            {
                var workItemId = await _azureDevOpsService.CreateWorkItemFromTemplateAsync(templateId, overrides);
                return Ok(workItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work item from template");
                return StatusCode(500, new { error = "Failed to create work item from template", details = ex.Message });
            }
        }

        [HttpDelete("templates/{templateId}")]
        public async Task<ActionResult> DeleteTemplate(int templateId)
        {
            try
            {
                await _azureDevOpsService.DeleteWorkItemTemplateAsync(templateId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting work item template");
                return StatusCode(500, new { error = "Failed to delete template", details = ex.Message });
            }
        }

        // Work Item Relations Endpoints
        [HttpPost("{workItemId}/relations")]
        public async Task<ActionResult<bool>> AddWorkItemRelation(int workItemId, [FromBody] WorkItemRelationRequest relation)
        {
            try
            {
                var result = await _azureDevOpsService.AddWorkItemRelationAsync(workItemId, relation);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding work item relation");
                return StatusCode(500, new { error = "Failed to add relation", details = ex.Message });
            }
        }

        [HttpDelete("{workItemId}/relations/{targetWorkItemId}")]
        public async Task<ActionResult<bool>> RemoveWorkItemRelation(int workItemId, int targetWorkItemId, [FromQuery] string relationType)
        {
            try
            {
                var result = await _azureDevOpsService.RemoveWorkItemRelationAsync(workItemId, targetWorkItemId, relationType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing work item relation");
                return StatusCode(500, new { error = "Failed to remove relation", details = ex.Message });
            }
        }

        [HttpGet("{workItemId}/relations")]
        public async Task<ActionResult<List<WorkItemRelationResponse>>> GetWorkItemRelations(int workItemId)
        {
            try
            {
                var relations = await _azureDevOpsService.GetWorkItemRelationsAsync(workItemId);
                return Ok(relations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving work item relations");
                return StatusCode(500, new { error = "Failed to retrieve relations", details = ex.Message });
            }
        }

        [HttpGet("{workItemId}/related")]
        public async Task<ActionResult<List<WorkItem>>> GetRelatedWorkItems(int workItemId, [FromQuery] string relationType)
        {
            try
            {
                var relatedWorkItems = await _azureDevOpsService.GetRelatedWorkItemsAsync(workItemId, relationType);
                return Ok(relatedWorkItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving related work items");
                return StatusCode(500, new { error = "Failed to retrieve related work items", details = ex.Message });
            }
        }
    }

    public class WorkItemUpdateRequest
    {
        public required string State { get; set; }
        public string? Comment { get; set; }
        public string? AssignedUser { get; set; }
        public int? Priority { get; set; }
        public double? RemainingEffortHours { get; set; }
        public double? CompletedEffortHours { get; set; }
        public string? Tag { get; set; }
    }

    internal class AzureDevOpsProcesses
    {
        public static readonly List<string> Processes = new List<string>
        {
        "Agile",
        "Scrum",
        "CMMI",
        "Basic",
        "MSF for Agile Software Development",
        "MSF for CMMI Process Improvement",
        "Upgrade",
        "Hosted XML",
        "Hosted",
        "Legacy Upgrade"
        };
    }
}
