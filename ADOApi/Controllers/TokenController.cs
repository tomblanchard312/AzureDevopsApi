using ADOApi.Models;
using ADOApi.Services;
using ADOApi.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace ADOApi.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "ADO.Admin")]
    public class TokenController : ControllerBase
    {
        private readonly IAzureDevOpsService _azureDevOpsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenController> _logger;

        public TokenController(IAzureDevOpsService azureDevOpsService, IConfiguration configuration, ILogger<TokenController> logger)
        {
            _azureDevOpsService = azureDevOpsService;
            _configuration = configuration;
            _logger = logger;
        }

        // POST /api/token/personalaccesstoken
        // Creates a PAT only when Features:EnablePatMinting=true. Requires ADO.Admin and second-factor (IP allowlist OR HMAC header).
        [HttpPost("personalaccesstoken")]
        public async Task<IActionResult> CreatePersonalAccessToken([FromBody] CreatePatRequest request)
        {
            // Prevent caching of sensitive responses
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            var enabled = _configuration.GetValue<bool>("Features:EnablePatMinting", false);
            if (!enabled)
            {
                // Hide endpoint when disabled
                return NotFound();
            }

            // Check second factor: IP allowlist OR HMAC-signed header
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var allowedIps = (_configuration["Features:PatAllowedIps"] ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var hasIpAllow = !string.IsNullOrEmpty(remoteIp) && allowedIps.Contains(remoteIp);

            var signatureHeader = Request.Headers.ContainsKey("X-Pat-Signature") ? Request.Headers["X-Pat-Signature"].ToString() : null;
            var hmacKey = _configuration["Features:PatHmacKey"];
            var hasValidHmac = false;
            if (!string.IsNullOrEmpty(signatureHeader) && !string.IsNullOrEmpty(hmacKey))
            {
                hasValidHmac = ValidateHmacSignature(request, signatureHeader, hmacKey);
            }

            if (!hasIpAllow && !hasValidHmac)
            {
                _logger.LogWarning("PAT minting denied: missing second factor. RemoteIp={RemoteIp}", remoteIp);
                return Forbid();
            }

            // Validate scopes against allowed list
            var allowedScopes = (_configuration["Features:AllowedPatScopes"] ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var requestedScopes = (request.Scope ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (allowedScopes.Length > 0 && requestedScopes.Except(allowedScopes).Any())
            {
                return BadRequest($"Requested scopes contain values not allowed. Allowed scopes: {string.Join(',', allowedScopes)}");
            }

            // Enforce maximum lifetime (days)
            var maxDays = _configuration.GetValue<int?>("Features:MaxPatLifetimeDays") ?? 7;
            var validTo = request.ValidTo ?? DateTime.UtcNow.AddDays(maxDays);
            if (validTo > DateTime.UtcNow.AddDays(maxDays))
            {
                return BadRequest($"Requested lifetime exceeds maximum of {maxDays} days");
            }

            // Create correlation id
            var correlationId = Request.Headers.ContainsKey("X-Correlation-Id") ? Request.Headers["X-Correlation-Id"].ToString() : Guid.NewGuid().ToString();

            try
            {
                // Create the PAT (service should return token value once)
                var token = await _azureDevOpsService.CreatePersonalAccessTokenAsync(
                    request.DisplayName,
                    request.Scope ?? string.Empty,
                    validTo,
                    false);

                // Store only metadata
                var metadata = new PatMetadata
                {
                    DisplayName = request.DisplayName,
                    Scope = request.Scope ?? string.Empty,
                    ValidTo = validTo,
                    AllOrgs = false,
                    CreatedBy = User?.Identity?.Name ?? "unknown",
                    RequesterIp = remoteIp,
                    CorrelationId = correlationId
                };
                await _azureDevOpsService.StorePatMetadataAsync(metadata);

                // Emit audit event
                _logger.LogInformation("PAT minted: {DisplayName} by {User}. Scopes={Scopes}, ValidTo={ValidTo}, RemoteIp={RemoteIp}, CorrelationId={CorrelationId}",
                    metadata.DisplayName, metadata.CreatedBy, metadata.Scope, metadata.ValidTo, metadata.RequesterIp, metadata.CorrelationId);

                // Return token to caller but do not persist it
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating personal access token");
                return StatusCode(500, "An internal error occurred");
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
                _logger.LogWarning(ex, "Invalid argument retrieving tokens");
                return BadRequest("Invalid request parameters");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving personal access tokens");
                return StatusCode(500, "An internal error occurred");
            }
        }

        private bool ValidateHmacSignature(CreatePatRequest request, string signatureHeader, string key)
        {
            try
            {
                var payload = $"{request.DisplayName}|{request.Scope}|{(request.ValidTo?.ToUniversalTime().ToString("o") ?? string.Empty)}";
                using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(key));
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
                var expected = Convert.ToBase64String(hash);
                return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(expected), System.Text.Encoding.UTF8.GetBytes(signatureHeader));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "HMAC validation failed");
                return false;
            }
        }

    }
}
