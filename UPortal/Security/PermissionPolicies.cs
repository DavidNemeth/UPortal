namespace UPortal.Security;

/// <summary>
/// Provides constants for permission-based authorization policies to avoid magic strings in the application.
/// Using these constants provides compile-time checking and IntelliSense.
/// </summary>
public static class PermissionPolicies
{
    private const string POLICY_PREFIX = "Require";
    private const string POLICY_SUFFIX = "Permission";

    // User Management
    public const string ManageUsers = $"{POLICY_PREFIX}ManageUsers{POLICY_SUFFIX}";
    public const string ViewUsers = $"{POLICY_PREFIX}ViewUsers{POLICY_SUFFIX}";
    public const string EditUsers = $"{POLICY_PREFIX}EditUsers{POLICY_SUFFIX}";

    // Role Management
    public const string ManageRoles = $"{POLICY_PREFIX}ManageRoles{POLICY_SUFFIX}";
    public const string ViewRoles = $"{POLICY_PREFIX}ViewRoles{POLICY_SUFFIX}";
    public const string AssignRoles = $"{POLICY_PREFIX}AssignRoles{POLICY_SUFFIX}";

    // Permission Management
    public const string ManagePermissions = $"{POLICY_PREFIX}ManagePermissions{POLICY_SUFFIX}";
    public const string ViewPermissions = $"{POLICY_PREFIX}ViewPermissions{POLICY_SUFFIX}";

    // Settings Management
    public const string ManageSettings = $"{POLICY_PREFIX}ManageSettings{POLICY_SUFFIX}";

    // General Access
    public const string AccessAdminPages = $"{POLICY_PREFIX}AccessAdminPages{POLICY_SUFFIX}";
    public const string ViewDashboard = $"{POLICY_PREFIX}ViewDashboard{POLICY_SUFFIX}";

    // Machine Management
    public const string ManageMachines = $"{POLICY_PREFIX}ManageMachines{POLICY_SUFFIX}";
    public const string ViewMachines = $"{POLICY_PREFIX}ViewMachines{POLICY_SUFFIX}";

    // Location Management
    public const string ManageLocations = $"{POLICY_PREFIX}ManageLocations{POLICY_SUFFIX}";
    public const string ViewLocations = $"{POLICY_PREFIX}ViewLocations{POLICY_SUFFIX}";

    // External Application Management
    public const string ManageExternalApplications = $"{POLICY_PREFIX}ManageExternalApplications{POLICY_SUFFIX}";
    public const string ViewExternalApplications = $"{POLICY_PREFIX}ViewExternalApplications{POLICY_SUFFIX}";
}