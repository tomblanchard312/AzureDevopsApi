using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ADOApi.Middleware
{
    // Populates correlation id and extracts actor info into HttpContext.Items
    public class AuditCorrelationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditCorrelationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Correlation id
            if (!context.Request.Headers.ContainsKey("X-Correlation-Id"))
            {
                context.Request.Headers["X-Correlation-Id"] = Guid.NewGuid().ToString();
            }

            context.Items["CorrelationId"] = context.Request.Headers["X-Correlation-Id"].ToString();

            // Actor info from claims (sub or oid, preferred)
            var user = context.User;
            var actorUpn = user?.Identity?.Name;
            var actorOid = user?.FindFirst("oid")?.Value ?? user?.FindFirst("sub")?.Value;
            context.Items["ActorUpn"] = actorUpn;
            context.Items["ActorObjectId"] = actorOid;

            // Client info
            context.Items["ClientIp"] = context.Connection.RemoteIpAddress?.ToString();
            context.Items["UserAgent"] = context.Request.Headers["User-Agent"].ToString();

            await _next(context);
        }
    }
}
