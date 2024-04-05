using System.Collections.Generic;
using System.Threading.Tasks;

using ADOApi.Models;

using ADOApi.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Organization.Client;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using ADOApi.Exceptions;
using ADOApi.Interfaces;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.TestManagement.TestPlanning.WebApi;
using WorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;
using Microsoft.TeamFoundation.Build.WebApi;

namespace ADOApi.Controllers
{

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class WorkItemController : ControllerBase
    {
        private readonly AzureDevOpsService _azureDevOpsService;
 
        public WorkItemController(AzureDevOpsService azureDevOpsService)
        {
            _azureDevOpsService = azureDevOpsService;
        }

        [HttpGet("workitemtypes")]
        public async Task<ActionResult<List<string>>> GetWorkItemTypes(string project)
        {
            try
            {
                List<string> workItemTypes = await _azureDevOpsService.GetWorkItemTypesAsync(project);
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
        public async Task<ActionResult<List<WorkItem>>> GetWorkItemsByType(string project, string workItemtype)
        {
            try
            {
                List<WorkItem> workItems = await _azureDevOpsService.GetWorkItemsByTypeAsync(project,workItemtype);
                return Ok(workItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPost("addworkitem")]
        public async Task<ActionResult<int>> AddWorkItem(WorkItemDetailsRequest request)
        {
            try
            {
                int parentId = 0;
                if (!string.IsNullOrEmpty(request.ParentWorkItemId))
                {
                    parentId = int.Parse(request.ParentWorkItemId);
                }

                int workItemId = await _azureDevOpsService.AddWorkItemAsync(
                    request.Project,
                    request.WorkItemType,
                    request.Title,
                    request.Description,
                    request.AssignedTo,
                    request.Tag,
                    request.EffortHours,
                    request.Comments,
                    parentId
                );

                return workItemId;
            }
            catch (Exception ex)
            {
                return BadRequest($"Error adding work item: {ex.Message}");
            }
        }       

        
        [HttpPut("updateworkitem")]
        public async Task<IActionResult> UpdateWorkItem(int workItemId, [FromBody] UpdateWorkItemModel model)
        {
            // Call the service method to update the work item
            bool success = await _azureDevOpsService.UpdateWorkItemAsync(
             workItemId,
             model.State,
             model.Comment,
             model.AssignedTo,
             model.Priority,
             model.RemainingEffortHours,
             model.CompletedEffortHours, model.Tag);
            if (success)
            {
                return Ok("Work item updated successfully.");
            }
            else
            {
                return BadRequest("Failed to update work item.");
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
    }
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
