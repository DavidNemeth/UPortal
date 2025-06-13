using Microsoft.EntityFrameworkCore;
using UPortal.Data;
using UPortal.Data.Models;
using UPortal.Dtos;
using System.Security.Claims;
using System.Linq;
using System; // Added for ArgumentNullException
using System.Collections.Generic; // Added for KeyNotFoundException

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
                .Select(u => new AppUserDto {
                    Id = u.Id,
                    Name = u.Name,
                    IsAdmin = u.IsAdmin,
                    IsActive = u.IsActive,
                    AzureAdObjectId = u.AzureAdObjectId
                })
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<AppUserDto?> GetByAzureAdObjectIdAsync(string azureAdObjectId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var appUser = await context.AppUsers
                .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAdObjectId);

            if (appUser == null)
            {
                return null;
            }

            return new AppUserDto
            {
                Id = appUser.Id,
                Name = appUser.Name,
                IsAdmin = appUser.IsAdmin,
                IsActive = appUser.IsActive,
                AzureAdObjectId = appUser.AzureAdObjectId
            };
        }

        public async Task<AppUserDto> CreateOrUpdateUserFromAzureAdAsync(ClaimsPrincipal userPrincipal)
        {
            if (userPrincipal == null)
            {
                throw new ArgumentNullException(nameof(userPrincipal));
            }

            var azureAdObjectId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                  userPrincipal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (string.IsNullOrEmpty(azureAdObjectId))
            {
                throw new ArgumentNullException(nameof(azureAdObjectId), "Azure AD Object ID not found in claims.");
            }

            var name = userPrincipal.Identity?.Name ?? "Unknown User";

            await using var context = await _contextFactory.CreateDbContextAsync();
            var appUser = await context.AppUsers
                .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAdObjectId);

            if (appUser == null)
            {
                // User does not exist, create new
                appUser = new AppUser
                {
                    AzureAdObjectId = azureAdObjectId,
                    Name = name,
                    IsAdmin = false, // Default for new users
                    IsActive = true,   // Default for new users
                    LocationId = 1    // Placeholder default LocationId
                };
                context.AppUsers.Add(appUser);
                await context.SaveChangesAsync();
            }
            // Optionally, update existing user's properties like Name if different
            // else if (appUser.Name != name)
            // {
            //     appUser.Name = name;
            //     await context.SaveChangesAsync();
            // }

            return new AppUserDto
            {
                Id = appUser.Id,
                Name = appUser.Name,
                IsAdmin = appUser.IsAdmin,
                IsActive = appUser.IsActive,
                AzureAdObjectId = appUser.AzureAdObjectId
            };
        }

        public async Task UpdateUserStatusAsync(int userId, bool isActive)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var appUser = await context.AppUsers.FindAsync(userId);

            if (appUser == null)
            {
                // Or handle as appropriate, e.g., return a status or log
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            appUser.IsActive = isActive;
            await context.SaveChangesAsync();
        }
    }
}