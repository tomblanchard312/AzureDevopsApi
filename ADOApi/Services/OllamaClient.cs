using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ADOApi.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ADOApi.Services
{
    public class OllamaClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;

        public OllamaClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("Ollama");
            _httpClient.BaseAddress = new Uri("http://localhost:11434");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _model = configuration["Ollama:Model"] ?? "qwen2.5-coder";
        }

        public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        {
            var prompt = $"{systemPrompt}\n\n{userPrompt}";

            var request = new
            {
                model = _model,
                prompt = prompt,
                stream = false
            };

            try
            {
                var jsonContent = JsonContent.Create(request);
                var response = await _httpClient.PostAsync("/api/generate", jsonContent, ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: ct);
                return result?.Response ?? string.Empty;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                throw new InvalidOperationException("Ollama service is not available or model is not loaded.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to generate response from Ollama.", ex);
            }
        }

        private class OllamaResponse
        {
            public string Response { get; set; } = string.Empty;
        }
    }
}