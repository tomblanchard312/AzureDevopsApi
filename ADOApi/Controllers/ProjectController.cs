using ADOApi.Services;
using ADOApi.Interfaces;
using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace ADOApi.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "ADO.ReadOnly")]
    public class ProjectController : ControllerBase
    {
        private readonly IAzureDevOpsService _azureDevOpsService;
        public ProjectController(IAzureDevOpsService azureDevOpsService)
        {
            _azureDevOpsService = azureDevOpsService;
        }

        [HttpGet("iterations")]
        public async Task<ActionResult<List<string>>> GetIterations(string project)
        {
            try
            {
                List<string> iterations = await _azureDevOpsService.GetIterationsAsync(project);

                var json = JsonSerializer.Serialize(iterations);
                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
                var etag = '"' + Convert.ToBase64String(hash) + '"';

                var clientEtag = Request.Headers["If-None-Match"].ToString();
                if (!string.IsNullOrEmpty(clientEtag) && clientEtag == etag)
                {
                    return StatusCode(304);
                }

                Response.Headers["ETag"] = etag;
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

                var json = JsonSerializer.Serialize(projects);
                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
                var etag = '"' + Convert.ToBase64String(hash) + '"';

                var clientEtag = Request.Headers["If-None-Match"].ToString();
                if (!string.IsNullOrEmpty(clientEtag) && clientEtag == etag)
                {
                    return StatusCode(304);
                }

                Response.Headers["ETag"] = etag;
                return Ok(projects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
