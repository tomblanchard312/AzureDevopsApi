using ADOApi.Services;

using Microsoft.AspNetCore.Mvc;

namespace ADOApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class ProjectController : Controller
    {
        private readonly AzureDevOpsService _azureDevOpsService;
        public ProjectController(AzureDevOpsService azureDevOpsService)
        {
            _azureDevOpsService = azureDevOpsService;
        }

        [HttpGet("iterations")]
        public async Task<ActionResult<List<string>>> GetIterations(string project)
        {
            try
            {
                List<string> iterations = await _azureDevOpsService.GetIterationsAsync(project);
                return Ok(iterations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpGet("projects")]
        public async Task<ActionResult<List<string>>> GetProjects()
        {
            try
            {
                List<string> projects = await _azureDevOpsService.GetProjectsAsync();
                return Ok(projects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
