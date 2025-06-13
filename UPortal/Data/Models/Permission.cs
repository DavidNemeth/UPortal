using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UPortal.Data.Models
{
    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)] // Permissions can have longer names, e.g., "User.Create"
        public string Name { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
