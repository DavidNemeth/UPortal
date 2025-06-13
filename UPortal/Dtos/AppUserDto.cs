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
        /// Gets or sets a value indicating whether the user has administrative privileges.
        /// </summary>
        public bool IsAdmin { get; set; }

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
    }

    /// <summary>
    /// Data Transfer Object for updating an application user's administrative settings.
    /// </summary>
    public class UpdateAppUserDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether the user should be an administrator.
        /// </summary>
        public bool IsAdmin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user account should be active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the ID of the new location for the user.
        /// </summary>
        public int LocationId { get; set; }
    }
}