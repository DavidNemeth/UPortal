using System.ComponentModel.DataAnnotations;

namespace UPortal.Data.Models
{
    /// <summary>
    /// Represents a physical or logical location within the organization.
    /// </summary>
    public class Location
    {
        /// <summary>
        /// Gets or sets the unique identifier for the location.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the location.
        /// This field is required and has a maximum length of 100 characters.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the collection of machines associated with this location.
        /// Initialized to an empty list to prevent null reference exceptions.
        /// </summary>
        public ICollection<Machine> Machines { get; set; } = new List<Machine>();

        /// <summary>
        /// Gets or sets the collection of application users associated with this location.
        /// Initialized to an empty list to prevent null reference exceptions.
        /// </summary>
        public ICollection<AppUser> AppUsers { get; set; } = new List<AppUser>();
    }
}
