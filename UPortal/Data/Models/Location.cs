using System.ComponentModel.DataAnnotations;

namespace UPortal.Data.Models
{
    public class Location
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        // Initialize collections to prevent NullReferenceExceptions
        public ICollection<Machine> Machines { get; set; } = new List<Machine>();
        public ICollection<AppUser> AppUsers { get; set; } = new List<AppUser>();
    }
}
