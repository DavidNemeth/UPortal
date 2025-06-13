using System.ComponentModel.DataAnnotations;

namespace UPortal.Data.Models
{
    /// <summary>
    /// Represents a machine or device within the organization.
    /// </summary>
    public class Machine
    {
        /// <summary>
        /// Gets or sets the unique identifier for the machine.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the machine.
        /// This field is required and has a maximum length of 50 characters.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the foreign key for the machine's location.
        /// </summary>
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the navigation property for the machine's location.
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// Gets or sets the foreign key for the user assigned to this machine.
        /// This is nullable, indicating that a machine may not be assigned to any user.
        /// </summary>
        public int? AppUserId { get; set; }

        /// <summary>
        /// Gets or sets the navigation property for the user assigned to this machine.
        /// This can be null if no user is assigned.
        /// </summary>
        public AppUser AppUser { get; set; }
    }
}
