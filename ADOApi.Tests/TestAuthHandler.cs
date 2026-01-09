using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If header not present, do not authenticate
        if (!Request.Headers.ContainsKey("Test-User"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var user = Request.Headers["Test-User"].FirstOrDefault() ?? "test-user";
        var rolesHeader = Request.Headers.ContainsKey("Test-Roles") ? Request.Headers["Test-Roles"].FirstOrDefault() : null;

        var claims = new[] { new Claim(ClaimTypes.Name, user) }.ToList();

        if (!string.IsNullOrEmpty(rolesHeader))
        {
            var roles = rolesHeader.Split(',').Select(r => r.Trim());
            foreach (var r in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, r));
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
