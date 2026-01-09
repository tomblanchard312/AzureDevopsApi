using System.Threading.Tasks;

namespace ADOApi.Interfaces
{
    public interface ISemanticChatService
    {
        Task<string> GetChatResponseAsync(string systemMessage, string userMessage);
    }
}
