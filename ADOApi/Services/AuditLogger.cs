using System.Threading.Tasks;
using ADOApi.Interfaces;
using ADOApi.Models;
using Microsoft.Extensions.Logging;
#if USE_APPINSIGHTS
using Microsoft.ApplicationInsights;
#endif

namespace ADOApi.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly ILogger<AuditLogger> _logger;
#if USE_APPINSIGHTS
        private readonly TelemetryClient? _telemetry;
        public AuditLogger(ILogger<AuditLogger> logger, TelemetryClient? telemetry = null)
        {
            _logger = logger;
            _telemetry = telemetry;
        }
#else
        public AuditLogger(ILogger<AuditLogger> logger)
        {
            _logger = logger;
        }
#endif

        public Task AuditAsync(AuditEvent evt)
        {
            // Structured log
            _logger.LogInformation(
                "Audit: Action={Action} Actor={ActorUpn} ActorId={ActorObjectId} Target={TargetType}/{TargetId} Project={Project} Repo={RepositoryId} WorkItem={WorkItemId} Success={Success} CorrelationId={CorrelationId} ClientIp={ClientIp} UserAgent={UserAgent} Error={Error}",
                evt.Action, evt.ActorUpn, evt.ActorObjectId, evt.TargetType, evt.TargetId, evt.Project, evt.RepositoryId, evt.WorkItemId, evt.Success, evt.CorrelationId, evt.ClientIp, evt.UserAgent, evt.ErrorMessage);

#if USE_APPINSIGHTS
            if (_telemetry != null)
            {
                var props = new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["action"] = evt.Action,
                    ["actor"] = evt.ActorUpn,
                    ["actorId"] = evt.ActorObjectId,
                    ["targetType"] = evt.TargetType,
                    ["targetId"] = evt.TargetId,
                    ["project"] = evt.Project,
                    ["repositoryId"] = evt.RepositoryId,
                    ["workItemId"] = evt.WorkItemId?.ToString(),
                    ["success"] = evt.Success.ToString(),
                    ["error"] = evt.ErrorMessage,
                    ["correlationId"] = evt.CorrelationId,
                    ["clientIp"] = evt.ClientIp,
                    ["userAgent"] = evt.UserAgent
                };
                _telemetry.TrackEvent("audit_event", props.ToDictionary(kv => kv.Key, kv => kv.Value ?? string.Empty));
            }
#endif

            return Task.CompletedTask;
        }
    }
}
