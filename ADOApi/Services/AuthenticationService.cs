using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;

namespace ADOApi.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(ILogger<AuthenticationService> logger)
        {
            _logger = logger;
        }
    }
}