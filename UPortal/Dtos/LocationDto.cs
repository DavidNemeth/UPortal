using System.ComponentModel.DataAnnotations;

namespace UPortal.Dtos
{
    public class LocationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int MachineCount { get; set; }
    }

    public class CreateLocationDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;
    }
}