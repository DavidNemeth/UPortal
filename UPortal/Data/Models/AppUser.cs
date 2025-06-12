using System.ComponentModel.DataAnnotations;

namespace UPortal.Data.Models
{
    public class AppUser
    {
        public int Id { get; set; }
        [Required]
        public string AzureAdObjectId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public bool IsAdmin { get; set; }

        public int LocationId { get; set; }
        public Location Location { get; set; }

        // Initialize collections to prevent NullReferenceExceptions
        public ICollection<Machine> Machines { get; set; } = new List<Machine>();
    }
}
