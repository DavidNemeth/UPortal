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
    }
}