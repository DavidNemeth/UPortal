using System.Collections.Generic;
using UPortal.Dtos;

namespace UPortal.Components.Dialogs
{
    public class UserRoleAssignmentDialogParameters
    {
        public AppUserDto User { get; set; } = null!;
        public List<int> SelectedRoleIds { get; set; } = new List<int>();
        public IEnumerable<RoleDto> AllRoles { get; set; } = new List<RoleDto>();
    }
}
