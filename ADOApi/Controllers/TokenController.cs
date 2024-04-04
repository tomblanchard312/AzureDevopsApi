using ADOApi.Models;
using ADOApi.Services;

using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace ADOApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly AzureDevOpsService _azureDevOpsService;

        public TokenController(AzureDevOpsService azureDevOpsService)
        {
            _azureDevOpsService = azureDevOpsService;
        }

        [HttpGet("personalaccesstoken")]
        public async Task<ActionResult<string>> GetPersonalAccessTokenAsync(string userName)
        {
            try
            {
                var personalAccessToken = await _azureDevOpsService.CreatePersonalAccessTokenAsync(userName, "vso.work_write vso.work", DateTime.Now.AddYears(1), false);
                return Ok(personalAccessToken);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("personalaccesstokens")]
        public async Task<ActionResult<List<PatResponse>>> GetTokensAsync()
        {
            try
            {
                List<PatResponse> pats = await _azureDevOpsService.GetTokensAsync();
                return Ok(pats);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
