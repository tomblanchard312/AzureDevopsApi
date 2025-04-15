using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ADOApi.Services
{
    public interface IWebhookService
    {
        Task RegisterWebhook(string url, string eventType, string secret);
        Task UnregisterWebhook(string url);
        Task NotifyEvent(string eventType, object payload);
    }

    public class WebhookService : IWebhookService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WebhookService> _logger;
        private readonly Dictionary<string, List<WebhookRegistration>> _webhooks;

        public WebhookService(IHttpClientFactory httpClientFactory, ILogger<WebhookService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _webhooks = new Dictionary<string, List<WebhookRegistration>>();
        }

        public Task RegisterWebhook(string url, string eventType, string secret)
        {
            if (!_webhooks.ContainsKey(eventType))
            {
                _webhooks[eventType] = new List<WebhookRegistration>();
            }

            _webhooks[eventType].Add(new WebhookRegistration
            {
                Url = url,
                Secret = secret
            });

            _logger.LogInformation($"Registered webhook for event {eventType} at {url}");
            return Task.CompletedTask;
        }

        public Task UnregisterWebhook(string url)
        {
            foreach (var eventType in _webhooks.Keys)
            {
                _webhooks[eventType].RemoveAll(w => w.Url == url);
            }

            _logger.LogInformation($"Unregistered webhook at {url}");
            return Task.CompletedTask;
        }

        public async Task NotifyEvent(string eventType, object payload)
        {
            if (!_webhooks.TryGetValue(eventType, out var registrations))
            {
                return;
            }

            var tasks = new List<Task>();
            foreach (var registration in registrations)
            {
                tasks.Add(SendWebhook(registration, eventType, payload));
            }

            await Task.WhenAll(tasks);
        }

        private async Task SendWebhook(WebhookRegistration registration, string eventType, object payload)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var json = JsonSerializer.Serialize(new
                {
                    EventType = eventType,
                    Timestamp = DateTime.UtcNow,
                    Payload = payload
                });

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(registration.Url, content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to send webhook to {registration.Url}. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending webhook to {registration.Url}");
            }
        }
    }

    public class WebhookRegistration
    {
        public string Url { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
    }
} 