using System.ComponentModel.DataAnnotations;

namespace UPortal.Dtos
{
    /// <summary>
    /// Data Transfer Object for location details, including counts of associated users and machines.
    /// </summary>
    public class LocationDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the location.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the location.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of users associated with this location.
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// Gets or sets the number of machines associated with this location.
        /// </summary>
        public int MachineCount { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for creating a new location.
    /// </summary>
    public class CreateLocationDto
    {
        /// <summary>
        /// Gets or sets the name of the new location.
        /// This field is required, with a minimum length of 2 and a maximum length of 100 characters.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;
    }
}