using UPortal.Dtos;

namespace UPortal.Services
{
    public interface IMachineService
    {
        Task<List<MachineDto>> GetAllAsync();
        Task<MachineDto> GetByIdAsync(int id);
        Task<MachineDto> CreateAsync(CreateMachineDto machineDto);
        Task<bool> UpdateAsync(int id, CreateMachineDto machineDto);
        Task<bool> DeleteAsync(int id);
    }
}