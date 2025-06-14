using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace UPortal.Security;

/// <summary>
/// A custom authorization policy provider that dynamically creates policies for permission-based authorization.
/// This provider generates policies based on a naming convention (e.g., "RequireManageUsersPermission")
/// without needing to register each policy individually at startup.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string POLICY_PREFIX = "Require";
    private const string POLICY_SUFFIX = "Permission";
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionPolicyProvider"/> class.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        // The DefaultAuthorizationPolicyProvider is used for any policies that
        // this custom provider doesn't handle, such as the fallback policy.
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();

    /// <summary>
    /// Gets an authorization policy by its name. If the policy name follows the required
    /// permission format, a new policy is created dynamically. Otherwise, it falls back
    /// to the default provider.
    /// </summary>
    /// <param name="policyName">The name of the policy to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// <see cref="AuthorizationPolicy"/> if a policy with the given name is found or created;
    /// otherwise, null.
    /// </returns>
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if the policy name matches our dynamic permission format.
        if (policyName.StartsWith(POLICY_PREFIX, StringComparison.OrdinalIgnoreCase) &&
            policyName.EndsWith(POLICY_SUFFIX, StringComparison.OrdinalIgnoreCase))
        {
            // Extract the permission name from the policy name.
            // e.g., "RequireManageUsersPermission" becomes "ManageUsers".
            var permissionName = policyName.Substring(POLICY_PREFIX.Length, policyName.Length - POLICY_PREFIX.Length - POLICY_SUFFIX.Length);

            // Create a new policy on-the-fly for this permission.
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermissionRequirement(permissionName));

            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        // If the policy name doesn't match our format, let the default provider handle it.
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}