using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ADOApi.Utilities
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore;
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            int maxRequests = 100,
            int timeWindowInSeconds = 60)
        {
            _next = next;
            _logger = logger;
            _maxRequests = maxRequests;
            _timeWindow = TimeSpan.FromSeconds(timeWindowInSeconds);
            _rateLimitStore = new ConcurrentDictionary<string, RateLimitInfo>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;

            var rateLimitInfo = _rateLimitStore.GetOrAdd(clientIp, _ => new RateLimitInfo
            {
                LastReset = now,
                RequestCount = 0
            });

            if (now - rateLimitInfo.LastReset > _timeWindow)
            {
                rateLimitInfo.LastReset = now;
                rateLimitInfo.RequestCount = 0;
            }

            if (rateLimitInfo.RequestCount >= _maxRequests)
            {
                _logger.LogWarning($"Rate limit exceeded for IP: {clientIp}");
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.Headers.Append("Retry-After", (_timeWindow - (now - rateLimitInfo.LastReset)).TotalSeconds.ToString());
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            rateLimitInfo.RequestCount++;
            await _next(context);
        }
    }

    public class RateLimitInfo
    {
        public DateTime LastReset { get; set; }
        public int RequestCount { get; set; }
    }
} 