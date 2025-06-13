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
                .Include(u => u.Location)
                .Select(u => new AppUserDto {
                    Id = u.Id,
                    Name = u.Name,
                    IsAdmin = u.IsAdmin,
                    IsActive = u.IsActive,
                    AzureAdObjectId = u.AzureAdObjectId,
                    LocationId = u.LocationId,
                    LocationName = u.Location != null ? u.Location.Name : string.Empty
                })
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<AppUserDto?> GetByAzureAdObjectIdAsync(string azureAdObjectId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var appUser = await context.AppUsers
                .Include(u => u.Location)
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
                AzureAdObjectId = appUser.AzureAdObjectId,
                LocationId = appUser.LocationId,
                LocationName = appUser.Location != null ? appUser.Location.Name : string.Empty
            };
        }

        public async Task<AppUserDto> CreateOrUpdateUserFromAzureAdAsync(ClaimsPrincipal userPrincipal)
        {
            if (userPrincipal == null)
            {
                throw new ArgumentNullException(nameof(userPrincipal));
            }

            var azureAdObjectId = userPrincipal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

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

            // Ensure Location is loaded if LocationId is set
            if (appUser.LocationId != 0 && appUser.Location == null)
            {
                appUser.Location = await context.Locations.FindAsync(appUser.LocationId);
            }

            return new AppUserDto
            {
                Id = appUser.Id,
                Name = appUser.Name,
                IsAdmin = appUser.IsAdmin,
                IsActive = appUser.IsActive,
                AzureAdObjectId = appUser.AzureAdObjectId,
                LocationId = appUser.LocationId,
                LocationName = appUser.Location != null ? appUser.Location.Name : string.Empty
            };
        }

        public async Task UpdateAppUserAsync(int userId, UpdateAppUserDto userToUpdate)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var appUser = await context.AppUsers.FindAsync(userId);

            if (appUser == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            appUser.IsActive = userToUpdate.IsActive;
            appUser.IsAdmin = userToUpdate.IsAdmin;
            appUser.LocationId = userToUpdate.LocationId; // Update LocationId

            await context.SaveChangesAsync();
        }
    }
}