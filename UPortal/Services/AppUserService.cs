using Microsoft.EntityFrameworkCore;
using UPortal.Data;
using UPortal.Dtos;

namespace UPortal.Services
{
    public class AppUserService : IAppUserService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public AppUserService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<AppUserDto>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AppUsers
                .Select(u => new AppUserDto { Id = u.Id, Name = u.Name })
                .OrderBy(u => u.Name)
                .ToListAsync();
        }
    }
}