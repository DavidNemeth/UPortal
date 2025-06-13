using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Collections.Generic; // Required for List<Claim>

namespace UPortal.Tests.Helpers
{
    public class TestAuthHandlerOptions : AuthenticationSchemeOptions
    {
        public string DefaultUserId { get; set; } = "test-user-id";
        public string DefaultAzureAdObjectId { get; set; } = "default-azure-ad-object-id"; // Added for AzureAdObjectId
        public string DefaultUserName { get; set; } = "Test User"; // Added for user name
        // Add other default claims as needed
    }

    public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
    {
        public const string AuthenticationScheme = "Test";

        public TestAuthHandler(
            IOptionsMonitor<TestAuthHandlerOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Options.DefaultUserId),
                new Claim(ClaimTypes.Name, Options.DefaultUserName),
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", Options.DefaultAzureAdObjectId),
                // Add other claims as needed by the application, e.g., roles
                // new Claim(ClaimTypes.Role, "Admin"),
            };
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
