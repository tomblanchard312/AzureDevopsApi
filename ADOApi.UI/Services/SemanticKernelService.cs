using System.Net.Http.Json;
using System.Text.Json;

namespace ADOApi.UI.Services;

public class SemanticKernelService
{
    private readonly HttpClient _httpClient;

    public SemanticKernelService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetAnswerAsync(string question)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/semantic-kernel/ask", new { question });
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<SemanticKernelResponse>();
            return result?.Answer ?? "No answer available.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}

public class SemanticKernelResponse
{
    public string Answer { get; set; } = string.Empty;
} 