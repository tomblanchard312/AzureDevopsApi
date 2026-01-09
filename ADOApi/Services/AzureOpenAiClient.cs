using System;
using System.Threading;
using System.Threading.Tasks;
using ADOApi.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ADOApi.Services
{
    public class AzureOpenAiClient : ILLMClient
    {
        private readonly Kernel _kernel;
        private readonly string _deployment;

        public AzureOpenAiClient(IConfiguration configuration)
        {
            var endpoint = configuration["OpenAI:Endpoint"];
            var apiKey = configuration["OpenAI:ApiKey"];
            _deployment = configuration["OpenAI:Deployment"] ?? string.Empty;

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(_deployment))
            {
                throw new InvalidOperationException("Azure OpenAI configuration is incomplete.");
            }

            _kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(_deployment, endpoint, apiKey)
                .Build();
        }

        public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        {
            var chatCompletionService = _kernel.Services.GetService<IChatCompletionService>();
            if (chatCompletionService == null)
            {
                throw new InvalidOperationException("Chat completion service not available.");
            }

            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(userPrompt);

            var result = await chatCompletionService.GetChatMessageContentAsync(history, cancellationToken: ct);
            return result.Content ?? string.Empty;
        }

        public string GetModelProvider() => "AzureOpenAI";

        public string GetModelName() => _deployment;
    }
}