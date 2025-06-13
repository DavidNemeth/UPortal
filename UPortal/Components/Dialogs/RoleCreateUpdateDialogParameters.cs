using System.Collections.Generic;
using UPortal.Dtos;

namespace UPortal.Components.Dialogs
{
    public class RoleCreateUpdateDialogParameters
    {
        public int Id { get; set; } // 0 for new role
        public string Name { get; set; } = string.Empty;
        public List<int> SelectedPermissionIds { get; set; } = new List<int>();
        public IEnumerable<PermissionDto> AllPermissions { get; set; } = new List<PermissionDto>();
    }
}
