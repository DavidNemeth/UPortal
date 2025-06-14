using UPortal.Dtos;

namespace UPortal.Services
{
    public interface IPermissionService
    {
        Task<List<PermissionDto>> GetAllPermissionsAsync();
        Task<PermissionDto> GetPermissionByIdAsync(int id);
        // Typically, permissions are predefined and not created/updated/deleted via UI in basic RBAC
        // But if needed:
        // Task<PermissionDto> CreatePermissionAsync(PermissionCreateDto permissionDto);
        // Task UpdatePermissionAsync(int id, PermissionUpdateDto permissionDto);
        // Task DeletePermissionAsync(int id);
    }
}
