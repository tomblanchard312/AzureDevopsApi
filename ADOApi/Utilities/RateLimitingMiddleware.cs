using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace ADOApi.Utilities
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IConfiguration _configuration;

        private readonly ConcurrentDictionary<string, RateLimitInfo> _store = new();
        private DateTime _lastCleanup = DateTime.UtcNow;
        private readonly object _cleanupLock = new();

        private readonly int _defaultMaxRequests;
        private readonly int _defaultWindowSeconds;
        private readonly int _aiMaxRequests;
        private readonly int _aiWindowSeconds;
        private readonly int _adminMaxRequests;
        private readonly int _adminWindowSeconds;
        private readonly long _maxRequestBodySize;
        private readonly int _maxQueryStringLength;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;

            _defaultMaxRequests = configuration.GetValue<int?>("RateLimiting:DefaultMaxRequests") ?? 100;
            _defaultWindowSeconds = configuration.GetValue<int?>("RateLimiting:DefaultWindowSeconds") ?? 60;

            _aiMaxRequests = configuration.GetValue<int?>("RateLimiting:AiMaxRequests") ?? 10;
            _aiWindowSeconds = configuration.GetValue<int?>("RateLimiting:AiWindowSeconds") ?? 60;

            _adminMaxRequests = configuration.GetValue<int?>("RateLimiting:AdminMaxRequests") ?? 30;
            _adminWindowSeconds = configuration.GetValue<int?>("RateLimiting:AdminWindowSeconds") ?? 60;

            _maxRequestBodySize = configuration.GetValue<long?>("RateLimiting:MaxRequestBodySizeBytes") ?? 5 * 1024 * 1024; // 5MB default
            _maxQueryStringLength = configuration.GetValue<int?>("RateLimiting:MaxQueryStringLength") ?? 2048;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var now = DateTime.UtcNow;

            // Periodically evict expired entries to prevent unbounded memory growth
            if (now - _lastCleanup > TimeSpan.FromMinutes(5))
            {
                lock (_cleanupLock)
                {
                    if (now - _lastCleanup > TimeSpan.FromMinutes(5))
                    {
                        _lastCleanup = now;
                        foreach (var kvp in _store)
                        {
                            if (now - kvp.Value.LastReset > TimeSpan.FromSeconds(kvp.Value.WindowSeconds * 2))
                            {
                                _store.TryRemove(kvp.Key, out _);
                            }
                        }
                    }
                }
            }

            // Quick checks: body size
            if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > _maxRequestBodySize)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsync("Request body too large");
                return;
            }

            // Query string length check
            var queryLength = context.Request.QueryString.HasValue ? context.Request.QueryString.Value.Length : 0;
            if (queryLength > _maxQueryStringLength)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Query string too long");
                return;
            }

            // Determine keys
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User?.Identity?.Name;

            // Determine which policy to apply
            bool isAi = context.Request.Path.HasValue && context.Request.Path.Value!.Contains("/SemanticKernel", StringComparison.OrdinalIgnoreCase);
            bool isAdmin = context.User?.IsInRole("ADO.Admin") == true || context.Request.Path.Value?.Contains("/templates", StringComparison.OrdinalIgnoreCase) == true && context.Request.Method == "DELETE";

            int userLimit = isAi ? _aiMaxRequests : (isAdmin ? _adminMaxRequests : _defaultMaxRequests);
            int userWindow = isAi ? _aiWindowSeconds : (isAdmin ? _adminWindowSeconds : _defaultWindowSeconds);

            int ipLimit = isAi ? Math.Max(5, _aiMaxRequests) : (isAdmin ? Math.Max(10, _adminMaxRequests) : _defaultMaxRequests);
            int ipWindow = userWindow;

            // Check user partition
            bool shouldDeny = false;
            int denyLimit = 0;
            DateTime denyReset = DateTime.MinValue;

            if (!string.IsNullOrEmpty(userId))
            {
                var userKey = $"user:{userId}:{userWindow}";
                var info = _store.GetOrAdd(userKey, _ => new RateLimitInfo { LastReset = now, RequestCount = 0, Limit = userLimit, WindowSeconds = userWindow });
                lock (info)
                {
                    if (now - info.LastReset > TimeSpan.FromSeconds(info.WindowSeconds))
                    {
                        info.LastReset = now;
                        info.RequestCount = 0;
                    }

                    if (info.RequestCount >= info.Limit)
                    {
                        shouldDeny = true;
                        denyLimit = info.Limit;
                        denyReset = info.LastReset.AddSeconds(info.WindowSeconds);
                    }
                    else
                    {
                        info.RequestCount++;
                    }

                    // add headers for user remaining
                    context.Response.OnStarting(() =>
                    {
                        var remaining = Math.Max(0, info.Limit - info.RequestCount);
                        context.Response.Headers["X-RateLimit-Limit"] = info.Limit.ToString();
                        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                        context.Response.Headers["X-RateLimit-Reset"] = ((int)(info.LastReset.AddSeconds(info.WindowSeconds) - now).TotalSeconds).ToString();
                        return Task.CompletedTask;
                    });
                }
                if (shouldDeny)
                {
                    _logger.LogWarning("User rate limit exceeded for {User}", userId);
                    await DenyAsync(context, denyLimit, 0, denyReset);
                    return;
                }
            }

            // Check IP partition
            var ipKey = $"ip:{ip}:{ipWindow}";
            var ipInfo = _store.GetOrAdd(ipKey, _ => new RateLimitInfo { LastReset = now, RequestCount = 0, Limit = ipLimit, WindowSeconds = ipWindow });
            bool ipShouldDeny = false;
            int ipDenyLimit = 0;
            DateTime ipDenyReset = DateTime.MinValue;
            lock (ipInfo)
            {
                if (now - ipInfo.LastReset > TimeSpan.FromSeconds(ipInfo.WindowSeconds))
                {
                    ipInfo.LastReset = now;
                    ipInfo.RequestCount = 0;
                }

                if (ipInfo.RequestCount >= ipInfo.Limit)
                {
                    ipShouldDeny = true;
                    ipDenyLimit = ipInfo.Limit;
                    ipDenyReset = ipInfo.LastReset.AddSeconds(ipInfo.WindowSeconds);
                }
                else
                {
                    ipInfo.RequestCount++;
                }

                context.Response.OnStarting(() =>
                {
                    var remaining = Math.Max(0, ipInfo.Limit - ipInfo.RequestCount);
                    // prefer existing headers if set by user partition; otherwise set IP headers
                    if (!context.Response.Headers.ContainsKey("X-RateLimit-Limit"))
                        context.Response.Headers["X-RateLimit-Limit"] = ipInfo.Limit.ToString();
                    if (!context.Response.Headers.ContainsKey("X-RateLimit-Remaining"))
                        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                    if (!context.Response.Headers.ContainsKey("X-RateLimit-Reset"))
                        context.Response.Headers["X-RateLimit-Reset"] = ((int)(ipInfo.LastReset.AddSeconds(ipInfo.WindowSeconds) - now).TotalSeconds).ToString();
                    return Task.CompletedTask;
                });
            }

            if (ipShouldDeny)
            {
                _logger.LogWarning("IP rate limit exceeded for {IP}", ip);
                await DenyAsync(context, ipDenyLimit, 0, ipDenyReset);
                return;
            }

            await _next(context);
        }

        private static Task DenyAsync(HttpContext context, int limit, int used, DateTime resetAt)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            var retryAfter = Math.Max(0, (int)(resetAt - DateTime.UtcNow).TotalSeconds);
            context.Response.Headers["Retry-After"] = retryAfter.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - used).ToString();
            return context.Response.WriteAsync("Rate limit exceeded");
        }
    }

    public class RateLimitInfo
    {
        public DateTime LastReset { get; set; }
        public int RequestCount { get; set; }
        public int Limit { get; set; }
        public int WindowSeconds { get; set; }
    }
    }