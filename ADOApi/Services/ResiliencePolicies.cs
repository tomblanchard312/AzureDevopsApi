using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ADOApi.Services
{
    public class ResiliencePolicies
    {
        private readonly ILogger<ResiliencePolicies> _logger;
        private readonly IConfiguration _configuration;

        private readonly AsyncPolicy _policyWrap;

        public ResiliencePolicies(ILogger<ResiliencePolicies> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            var retries = configuration.GetValue<int?>("Resilience:RetryCount") ?? 3;
            var baseDelaySeconds = configuration.GetValue<int?>("Resilience:BaseDelaySeconds") ?? 2;
            var breakerFailures = configuration.GetValue<int?>("Resilience:CircuitFailures") ?? 5;
            var breakerDurationSeconds = configuration.GetValue<int?>("Resilience:CircuitDurationSeconds") ?? 30;
            var timeoutSeconds = configuration.GetValue<int?>("Resilience:TimeoutSeconds") ?? 10;

            var jitterer = new Random();

            // Retry with jittered backoff and respect Retry-After when possible
            var retryPolicy = Policy.Handle<Exception>(ShouldRetry)
                .WaitAndRetryAsync(retries, attempt =>
                {
                    // best-effort jittered backoff; honor 429 with longer default
                    var jitter = TimeSpan.FromMilliseconds(jitterer.Next(0, 1000));
                    return TimeSpan.FromSeconds(Math.Pow(baseDelaySeconds, attempt)) + jitter;
                });

            // Circuit breaker
            var circuit = Policy.Handle<Exception>(ShouldRetry)
                .CircuitBreakerAsync(breakerFailures, TimeSpan.FromSeconds(breakerDurationSeconds), onBreak: (ex, ts) =>
                {
                    _logger.LogWarning(ex, "Circuit broken for {Duration}s due to {Message}", ts.TotalSeconds, ex.Message);
                }, onReset: () =>
                {
                    _logger.LogInformation("Circuit reset");
                }, onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit half-open: testing...");
                });

            // Timeout
            var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(timeoutSeconds), TimeoutStrategy.Pessimistic, onTimeoutAsync: (ctx, t, task) =>
            {
                _logger.LogWarning("Operation timed out after {Timeout}s", timeoutSeconds);
                return Task.CompletedTask;
            });

            _policyWrap = Policy.WrapAsync(retryPolicy, circuit, timeout);
        }

        private bool ShouldRetry(Exception ex)
        {
            // Do not retry on argument or contract errors
            if (ex is ArgumentException || ex is UnauthorizedAccessException)
                return false;

            // Http failures
            if (ex is HttpRequestException hre)
            {
                if (hre.StatusCode.HasValue)
                {
                    var code = (int)hre.StatusCode.Value;
                    // Retry on 429 and 5xx
                    if (code == 429 || code >= 500)
                        return true;
                    // Do not retry on other 4xx
                    if (code >= 400 && code < 500)
                        return false;
                }

                // Unknown status - allow retry
                return true;
            }

            // Timeout and cancellation are retriable
            if (ex is TaskCanceledException || ex is TimeoutRejectedException)
                return true;

            // For Vss exceptions etc., be conservative and retry
            return true;
        }

        public Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            return _policyWrap.ExecuteAsync(operation);
        }

        public Task ExecuteAsync(Func<Task> operation)
        {
            return _policyWrap.ExecuteAsync(operation);
        }
    }
}
