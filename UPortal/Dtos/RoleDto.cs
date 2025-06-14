using System.Collections.Generic;

namespace UPortal.Dtos
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    }
}
