using System.ComponentModel.DataAnnotations;

namespace UPortal.Dtos
{
    /// <summary>
    /// Data Transfer Object for machine details.
    /// </summary>
    public class MachineDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the machine.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the machine.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the machine's location.
        /// </summary>
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the user assigned to this machine.
        /// Can be null or empty if the machine is unassigned.
        /// </summary>
        public string? AssignedUserName { get; set; } // Can be null if unassigned

        /// <summary>
        /// Gets or sets the ID of the machine's location.
        /// A valid location must be selected.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "A valid location must be selected.")]
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user assigned to this machine.
        /// This is nullable if the machine is unassigned.
        /// </summary>
        public int? AppUserId { get; set; } // Nullable for unassigned machines
    }

    /// <summary>
    /// Data Transfer Object for creating a new machine.
    /// </summary>
    public class CreateMachineDto
    {
        /// <summary>
        /// Gets or sets the name of the new machine.
        /// This field is required, with a minimum length of 2 and a maximum length of 100 characters.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the location for the new machine.
        /// A valid location must be selected.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "A valid location must be selected.")]
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user to be assigned to the new machine.
        /// This is nullable, allowing the machine to be initially unassigned.
        /// </summary>
        public int? AppUserId { get; set; } // Nullable for unassigned machines
    }
}