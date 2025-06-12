using System.ComponentModel.DataAnnotations;

namespace UPortal.Dtos
{
    public class MachineDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string? AssignedUserName { get; set; } // Can be null if unassigned
        [Range(1, int.MaxValue, ErrorMessage = "A valid location must be selected.")]
        public int LocationId { get; set; }

        public int? AppUserId { get; set; } // Nullable for unassigned machines
    }

    public class CreateMachineDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "A valid location must be selected.")]
        public int LocationId { get; set; }

        public int? AppUserId { get; set; } // Nullable for unassigned machines
    }
}