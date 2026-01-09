using System.Threading;
using System.Threading.Tasks;

namespace ADOApi.Interfaces
{
    public interface ILLMClient
    {
        Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
    }
}