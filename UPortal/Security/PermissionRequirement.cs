// UPortal/Security/PermissionRequirement.cs
using Microsoft.AspNetCore.Authorization;

namespace UPortal.Security
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
