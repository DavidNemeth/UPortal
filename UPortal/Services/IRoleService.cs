using System.Collections.Generic;
using System.Threading.Tasks;
using UPortal.Dtos;

namespace UPortal.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
        Task<RoleDto> GetRoleByIdAsync(int id);
        Task<RoleDto> CreateRoleAsync(RoleCreateDto roleDto);
        Task UpdateRoleAsync(int id, RoleUpdateDto roleDto);
        Task DeleteRoleAsync(int id);
        Task AssignPermissionToRoleAsync(int roleId, int permissionId);
        Task RemovePermissionFromRoleAsync(int roleId, int permissionId);
        Task<IEnumerable<PermissionDto>> GetPermissionsForRoleAsync(int roleId);
    }
}
