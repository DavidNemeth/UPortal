using System.ComponentModel.DataAnnotations;

namespace UPortal.Data.Models
{
    public class Machine
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        public int LocationId { get; set; }
        public Location Location { get; set; }

        public int? AppUserId { get; set; }
        public AppUser AppUser { get; set; }
    }
}
