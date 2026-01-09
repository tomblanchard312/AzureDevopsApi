using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ADOApi.Interfaces;
using ADOApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ADOApi.Services
{
    public class RiskAcceptanceExpiryService : BackgroundService
    {
        private readonly ILogger<RiskAcceptanceExpiryService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostEnvironment _environment;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Daily check

        public RiskAcceptanceExpiryService(
            ILogger<RiskAcceptanceExpiryService> logger,
            IServiceProvider serviceProvider,
            IHostEnvironment environment)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _environment = environment;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogInformation("Risk Acceptance Expiry Service skipped in development mode");
                return;
            }

            _logger.LogInformation("Risk Acceptance Expiry Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckExpiringAcceptancesAsync();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in risk acceptance expiry check");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry after 5 minutes on error
                }
            }
        }

        private async Task CheckExpiringAcceptancesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var securityAdvisorService = scope.ServiceProvider.GetRequiredService<ISecurityAdvisorService>();

            var expiringDate = DateTime.UtcNow.AddDays(14);
            var expiringAcceptances = await securityAdvisorService.GetExpiringRiskAcceptancesAsync(expiringDate);

            foreach (var acceptance in expiringAcceptances)
            {
                // Log security event for expiring acceptance
                await securityAdvisorService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = "risk_acceptance_expiring",
                    Organization = acceptance.Organization,
                    Project = acceptance.Project,
                    Repository = acceptance.Repository,
                    FindingId = acceptance.FindingId,
                    UserId = "system",
                    UserRole = "system",
                    Timestamp = DateTime.UtcNow,
                    PromptVersion = "system",
                    PolicyVersion = "system",
                    Properties = new Dictionary<string, object>
                    {
                        ["acceptanceId"] = acceptance.Id,
                        ["acceptedBy"] = acceptance.AcceptedBy,
                        ["expiresAt"] = acceptance.ExpiresAt,
                        ["daysUntilExpiry"] = (acceptance.ExpiresAt - DateTime.UtcNow)?.TotalDays ?? 0
                    }
                });

                // TODO: Post warning comment to PR if still open
                // This would require integrating with Azure DevOps PR APIs
                _logger.LogWarning("Risk acceptance {AcceptanceId} for finding {FindingId} expires on {ExpiresAt}",
                    acceptance.Id, acceptance.FindingId, acceptance.ExpiresAt);
            }

            if (expiringAcceptances.Any())
            {
                _logger.LogInformation("Found {Count} risk acceptances expiring within 14 days", expiringAcceptances.Count);
            }
        }
    }
}