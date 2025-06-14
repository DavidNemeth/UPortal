namespace UPortal.Dtos
{
    /// <summary>
    /// Data Transfer Object for application user details.
    /// </summary>
    public class AppUserDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the user account is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the Azure Active Directory Object ID for the user.
        /// </summary>
        public string AzureAdObjectId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the user's location.
        /// </summary>
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the name of the user's location.
        /// </summary>
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the roles assigned to the user.
        /// </summary>
        public List<RoleDto> Roles { get; set; } = new List<RoleDto>();
    }

    /// <summary>
    /// Data Transfer Object for updating an application user's administrative settings.
    /// </summary>
    public class UpdateAppUserDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether the user account should be active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the ID of the new location for the user.
        /// </summary>
        public int LocationId { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for managing a user's role assignments in a dialog.
    /// It only contains the list of role IDs being managed.
    /// </summary>
    public class AssignUserRolesDto
    {
        public List<int> RoleIds { get; set; } = new();
    }

}