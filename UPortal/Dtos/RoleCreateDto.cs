using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UPortal.Dtos
{
    public class RoleCreateDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        public List<int> PermissionIds { get; set; } = new List<int>();
    }
}
