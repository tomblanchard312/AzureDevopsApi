using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using ADOApi.Interfaces;
using ADOApi.Models;

namespace ADOApi.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        public async Task<string> CreatePersonalAccessTokenAsync(string displayName, string scope, DateTime validTo, bool allOrgs, HttpClient httpClient, string organization, string adminToken)
        {
            try
            {
                string baseUrl = $"https://dev.azure.com/{organization}/";
                var requestBody = new
                {
                    displayName,
                    scope,
                    validTo = validTo.ToUniversalTime().ToString("O"),
                    allOrgs
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var requestData = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{adminToken}")));

                var response = await httpClient.PostAsync($"{baseUrl}_apis/tokens/pats?api-version=7.0", requestData);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var patResponse = JsonSerializer.Deserialize<PatResponse>(responseContent);

                return patResponse.AccessToken;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception($"Failed to create personal access token: {ex.Message}", ex);
            }
        }

        public async Task<List<PatResponse>> GetTokensAsync(HttpClient httpClient, string organization, string adminToken)
        {
            try
            {
                string baseUrl = $"https://dev.azure.com/{organization}/";
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{adminToken}")));

                var response = await httpClient.GetAsync($"{baseUrl}_apis/tokens/pats?api-version=7.0");
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var pats = JsonSerializer.Deserialize<List<PatResponse>>(responseContent);

                return pats;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception($"Failed to retrieve personal access tokens: {ex.Message}", ex);
            }
        }
    }
}