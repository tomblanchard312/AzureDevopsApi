using ADOApi.Models;

namespace ADOApi.Interfaces
{
    public interface IAuthenticationService
    {
        Task<string> CreatePersonalAccessTokenAsync(string displayName, string scope, DateTime validTo, bool allOrgs, HttpClient httpClient, string organization, string adminToken);
        Task<List<PatResponse>> GetTokensAsync(HttpClient httpClient, string organization, string adminToken);
    }
}
