using System.Threading.Tasks;
using ADOApi.Interfaces;

namespace ADOApi.Services
{
    public class SemanticChatService : ISemanticChatService
    {
        private readonly ILLMClient _llmClient;

        public SemanticChatService(ILLMClient llmClient)
        {
            _llmClient = llmClient;
        }

        public async Task<string> GetChatResponseAsync(string systemMessage, string userMessage)
        {
            try
            {
                return await _llmClient.GenerateAsync(systemMessage, userMessage);
            }
            catch (Exception ex)
            {
                return $"AI service error: {ex.Message}";
            }
        }
    }
}
