using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ADOApi.Interfaces;
using System.Threading.Tasks;

namespace ADOApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Policy = "ADO.Admin")]
    public class HealthController : ControllerBase
    {
        private readonly IAzureDevOpsConnectionFactory _connectionFactory;

        public HealthController(IAzureDevOpsConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        [HttpGet("azuredevops")]
        public async Task<IActionResult> CheckAzureDevOpsConnectivity()
        {
            try
            {
                var connection = await _connectionFactory.CreateConnectionAsync();
                // Try to get a client to validate the connection
                var projectClient = connection.GetClient<Microsoft.TeamFoundation.Core.WebApi.ProjectHttpClient>();
                var projects = await projectClient.GetProjects();
                return Ok(new { status = "OK", message = "Azure DevOps connectivity verified" });
            }
            catch (Exception)
            {
                return StatusCode(503, new { status = "Fail", message = "Azure DevOps connectivity check failed" });
            }
        }
    }
}