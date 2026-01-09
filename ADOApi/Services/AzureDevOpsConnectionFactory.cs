using System;
using System.Threading;
using System.Threading.Tasks;
using ADOApi.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADOApi.Services
{
    public class AzureDevOpsConnectionFactory : IAzureDevOpsConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureDevOpsConnectionFactory> _logger;
        private readonly IConfidentialClientApplication _msalClient;
        private string? _cachedToken;
        private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

        public AzureDevOpsConnectionFactory(IConfiguration configuration, ILogger<AzureDevOpsConnectionFactory> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var entraConfig = configuration.GetSection("AzureDevOpsEntra");
            var clientId = entraConfig["ClientId"];
            var clientSecret = entraConfig["ClientSecret"];
            var authorityHost = entraConfig["AuthorityHost"] ?? "https://login.microsoftonline.com/";
            var tenantId = entraConfig["TenantId"];

            // Only create MSAL client if values are not placeholder values
            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(tenantId)
                && !clientId.Contains("[") && !clientSecret.Contains("[") && !tenantId.Contains("["))
            {
                _msalClient = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority($"{authorityHost}{tenantId}")
                    .Build();
            }
        }

        public async Task<VssConnection> CreateConnectionAsync(CancellationToken ct = default)
        {
            var organizationUrl = _configuration["AzureDevOps:OrganizationUrl"];
            if (string.IsNullOrEmpty(organizationUrl))
            {
                throw new InvalidOperationException("AzureDevOps:OrganizationUrl is not configured.");
            }

            var useEntraAuth = _configuration.GetValue<bool>("AzureDevOps:UseEntraAuth");

            if (useEntraAuth && _msalClient != null)
            {
                var token = await GetEntraTokenAsync(ct);
                var credentials = new VssOAuthAccessTokenCredential(token);
                return new VssConnection(new Uri(organizationUrl), credentials);
            }
            else
            {
                // Fall back to PAT authentication
                var pat = _configuration["AzureDevOps:PersonalAccessToken"];
                if (string.IsNullOrEmpty(pat) || pat.Contains("["))
                {
                    // For development, use a dummy PAT if not configured
                    pat = "dummy-pat-for-development";
                    _logger.LogWarning("Using dummy PAT for development. Configure AzureDevOps:PersonalAccessToken for production use.");
                }
                var credentials = new VssBasicCredential(string.Empty, pat);
                return new VssConnection(new Uri(organizationUrl), credentials);
            }
        }

        private async Task<string> GetEntraTokenAsync(CancellationToken ct)
        {
            if (_msalClient == null)
            {
                throw new InvalidOperationException("MSAL client not configured for Entra authentication.");
            }

            if (_cachedToken != null && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-5))
            {
                return _cachedToken;
            }

            var scopes = _configuration.GetSection("AzureDevOpsEntra:Scopes").Get<string[]>() ?? new[] { "499b84ac-1321-427f-aa17-267ca6975798/.default" };

            try
            {
                var result = await _msalClient.AcquireTokenForClient(scopes).ExecuteAsync(ct);
                _cachedToken = result.AccessToken;
                _tokenExpiry = result.ExpiresOn;
                return _cachedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire Entra token for Azure DevOps");
                throw new InvalidOperationException("Failed to acquire authentication token.", ex);
            }
        }
    }
}