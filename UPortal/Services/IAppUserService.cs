using UPortal.Dtos;

namespace UPortal.Services
{
    public interface IAppUserService
    {
        Task<List<AppUserDto>> GetAllAsync();
    }
}