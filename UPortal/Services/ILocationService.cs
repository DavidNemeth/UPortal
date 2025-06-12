using UPortal.Dtos;

namespace UPortal.Services
{
    public interface ILocationService
    {
        Task<List<LocationDto>> GetAllAsync();
        Task<LocationDto?> GetByIdAsync(int id);
        Task<LocationDto> CreateAsync(CreateLocationDto locationDto);
        Task<bool> UpdateAsync(int id, CreateLocationDto locationDto);
        Task<bool> DeleteAsync(int id);
    }
}