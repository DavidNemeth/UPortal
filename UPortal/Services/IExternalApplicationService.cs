using System.Collections.Generic;
using System.Threading.Tasks;
using UPortal.Dtos;

namespace UPortal.Services
{
    public interface IExternalApplicationService
    {
        Task<List<ExternalApplicationDto>> GetAllAsync();
        Task AddAsync(ExternalApplicationDto externalApplication);
        Task DeleteAsync(int id);
    }
}
