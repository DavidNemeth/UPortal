using System.Collections.Generic;
using System.Threading.Tasks;
using UPortal.Dtos;

namespace UPortal.Services
{
    public interface IExternalApplicationService
    {
        Task<List<ExternalApplicationDto>> GetAllAsync();
           Task<ExternalApplicationDto?> GetByIdAsync(int id); // Add this line
        Task AddAsync(ExternalApplicationDto externalApplication);
        Task DeleteAsync(int id);
    }
}
