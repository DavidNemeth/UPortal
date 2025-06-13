// UPortal/Security/PermissionHandler.cs
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using UPortal.Services; // For IAppUserService
using Microsoft.Extensions.Logging; // For ILogger

namespace UPortal.Security
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IAppUserService _appUserService;
        private readonly ILogger<PermissionHandler> _logger;

        public PermissionHandler(IAppUserService appUserService, ILogger<PermissionHandler> logger)
        {
            _appUserService = appUserService;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var userIdClaim = context.User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                           ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var appUserId))
            {
                var azureAdObjectId = context.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
                if (string.IsNullOrEmpty(azureAdObjectId))
                {
                    _logger.LogWarning("User ID claim (nameidentifier) and Azure AD Object ID claim not found. Cannot verify permission '{Permission}'.", requirement.Permission);
                    context.Fail(); // No way to identify the user from our DB
                    return;
                }

                _logger.LogDebug("User ID claim (nameidentifier) is missing or not an integer. Trying to find user by Azure AD Object ID: {AzureAdObjectId}", azureAdObjectId);
                var user = await _appUserService.GetByAzureAdObjectIdAsync(azureAdObjectId);
                if (user == null) {
                    _logger.LogWarning("User with Azure AD Object ID '{AzureAdObjectId}' not found in local DB. Cannot verify permission '{Permission}'.", azureAdObjectId, requirement.Permission);
                    context.Fail();
                    return;
                }
                appUserId = user.Id;
                _logger.LogDebug("User found by Azure AD Object ID. AppUser ID: {AppUserId}", appUserId);
            }

            _logger.LogDebug("Checking permission '{Permission}' for user ID '{AppUserId}'.", requirement.Permission, appUserId);

            if (await _appUserService.UserHasPermissionAsync(appUserId, requirement.Permission))
            {
                _logger.LogInformation("User ID '{AppUserId}' has permission '{Permission}'. Requirement succeeded.", appUserId, requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("User ID '{AppUserId}' does NOT have permission '{Permission}'. Requirement failed.", appUserId, requirement.Permission);
                // context.Fail() is implicitly called if Succeed is not.
            }
        }
    }
}
