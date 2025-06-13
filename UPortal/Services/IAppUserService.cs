using UPortal.Dtos;
using System.Security.Claims;

namespace UPortal.Services
{
    public interface IAppUserService
    {
        Task<List<AppUserDto>> GetAllAsync();
        Task<AppUserDto?> GetByAzureAdObjectIdAsync(string azureAdObjectId);
        Task<AppUserDto> CreateOrUpdateUserFromAzureAdAsync(System.Security.Claims.ClaimsPrincipal userPrincipal);
        Task UpdateAppUserAsync(int userId, UpdateAppUserDto userToUpdate);
        Task AssignRoleToUserAsync(int userId, int roleId);
        Task RemoveRoleFromUserAsync(int userId, int roleId);
        Task<IEnumerable<RoleDto>> GetRolesForUserAsync(int userId);
        Task<bool> UserHasPermissionAsync(int userId, string permissionName);
        Task<bool> UserHasRoleAsync(int userId, string roleName);
    }
}