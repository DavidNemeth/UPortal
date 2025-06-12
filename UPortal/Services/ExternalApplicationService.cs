using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UPortal.Data;
using UPortal.Data.Models;
using UPortal.Dtos;

namespace UPortal.Services
{
    public class ExternalApplicationService : IExternalApplicationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public ExternalApplicationService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<ExternalApplicationDto>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ExternalApplications
                .Select(e => new ExternalApplicationDto
                {
                    Id = e.Id,
                    AppName = e.AppName,
                    AppUrl = e.AppUrl,
                    IconName = e.IconName
                })
                .OrderBy(e => e.AppName)
                .ToListAsync();
        }

           public async Task<ExternalApplicationDto?> GetByIdAsync(int id) // Add this method
           {
               await using var context = await _contextFactory.CreateDbContextAsync();
               var app = await context.ExternalApplications.FindAsync(id);
               if (app == null)
               {
                   return null;
               }
               return new ExternalApplicationDto
               {
                   Id = app.Id,
                   AppName = app.AppName,
                   AppUrl = app.AppUrl,
                   IconName = app.IconName
               };
           }

        public async Task AddAsync(ExternalApplicationDto externalApplicationDto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var externalApplication = new ExternalApplication
            {
                AppName = externalApplicationDto.AppName,
                AppUrl = externalApplicationDto.AppUrl,
                IconName = externalApplicationDto.IconName
            };
            context.ExternalApplications.Add(externalApplication);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var externalApplication = await context.ExternalApplications.FindAsync(id);
            if (externalApplication != null)
            {
                context.ExternalApplications.Remove(externalApplication);
                await context.SaveChangesAsync();
            }
        }
    }
}
