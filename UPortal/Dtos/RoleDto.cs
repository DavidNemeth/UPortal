using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UPortal.Dtos
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    }
    public class CreateRoleDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        public List<int> PermissionIds { get; set; } = new List<int>();
    }

    public class RoleUpdateDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        public List<int> PermissionIds { get; set; } = new List<int>();
    }
}
