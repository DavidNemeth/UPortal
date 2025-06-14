using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UPortal.Data;
using UPortal.Dtos;
using UPortal.Data.Models; // Required for Permission model

namespace UPortal.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<PermissionService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync()
        {
            _logger.LogInformation("GetAllPermissionsAsync called");
            await using var context = await _contextFactory.CreateDbContextAsync();
            var permissions = await context.Permissions
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name
                })
                .OrderBy(p => p.Name)
                .ToListAsync();
            _logger.LogInformation("GetAllPermissionsAsync completed, returning {PermissionCount} permissions.", permissions.Count);
            return permissions;
        }

        public async Task<PermissionDto> GetPermissionByIdAsync(int id)
        {
            _logger.LogInformation("GetPermissionByIdAsync called with Id: {Id}", id);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var permission = await context.Permissions
                .Where(p => p.Id == id)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name
                })
                .FirstOrDefaultAsync();

            if (permission == null)
            {
                _logger.LogWarning("Permission with Id: {Id} not found.", id);
                // Consider throwing a KeyNotFoundException or returning null based on desired contract
                return null;
            }

            _logger.LogInformation("GetPermissionByIdAsync completed, returning permission: {PermissionName}", permission.Name);
            return permission;
        }
    }
}
