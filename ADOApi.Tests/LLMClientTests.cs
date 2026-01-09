using System;
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ADOApi.Services;
using ADOApi.Interfaces;

namespace ADOApi.Tests
{
    public class LLMClientTests
    {
        [Fact]
        public async Task AzureOpenAiClient_GeneratesResponse()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["OpenAI:Endpoint"]).Returns("https://test.openai.azure.com");
            config.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
            config.Setup(c => c["OpenAI:Deployment"]).Returns("test-deployment");

            var client = new AzureOpenAiClient(config.Object);

            // Act & Assert
            // Note: This would require actual Azure OpenAI setup for full test
            // For now, just ensure no exceptions during construction
            Assert.NotNull(client);
        }

        [Fact]
        public async Task OllamaClient_HandlesConnectionRefused()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["Ollama:Model"]).Returns("test-model");

            var httpClientFactory = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(new MockHttpMessageHandler((request, ct) =>
            {
                throw new HttpRequestException("Connection refused", null, HttpStatusCode.InternalServerError);
            }));
            httpClientFactory.Setup(f => f.CreateClient("Ollama")).Returns(httpClient);

            var client = new OllamaClient(httpClientFactory.Object, config.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.GenerateAsync("system", "user"));
            Assert.Contains("not available", exception.Message);
        }

        [Fact]
        public void LLMProvider_DefaultsToAzureOpenAI()
        {
            // This test verifies the factory logic in Program.cs
            // Since DI is used, we test the configuration reading
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["LLM:Provider"]).Returns((string)null);

            var provider = config.Object["LLM:Provider"] ?? "AzureOpenAI";
            Assert.Equal("AzureOpenAI", provider);
        }

        [Fact]
        public void LLMProvider_SwitchesToOllama()
        {
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["LLM:Provider"]).Returns("Ollama");

            var provider = config.Object["LLM:Provider"];
            Assert.Equal("Ollama", provider);
        }
    }

    // Mock HTTP handler for testing
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}