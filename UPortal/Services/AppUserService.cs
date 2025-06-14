using Microsoft.EntityFrameworkCore;
using UPortal.Data;
using UPortal.Data.Models;
using UPortal.Dtos;
using System.Security.Claims;

namespace UPortal.Services
{
    /// <summary>
    /// Service for managing application users.
    /// </summary>
    public class AppUserService : IAppUserService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<AppUserService> _logger;

        public AppUserService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<AppUserService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<List<AppUserDto>> GetAllAsync()
        {
            _logger.LogInformation("GetAllAsync called - fetching all users with their roles.");
            await using var context = await _contextFactory.CreateDbContextAsync();
            var users = await context.AppUsers
                .Include(u => u.Location)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .OrderBy(u => u.Name)
                .ToListAsync();

            var userDtos = users.Select(u => new AppUserDto
            {
                Id = u.Id,
                Name = u.Name,
                IsActive = u.IsActive,
                AzureAdObjectId = u.AzureAdObjectId,
                LocationId = u.LocationId,
                LocationName = u.Location != null ? u.Location.Name : string.Empty,
                Roles = u.UserRoles.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Permissions = ur.Role.RolePermissions.Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name
                    }).ToList()
                }).ToList()
            }).ToList();

            _logger.LogInformation("GetAllAsync completed, returning {UserCount} users.", userDtos.Count);
            return userDtos;
        }

        /// <inheritdoc />
        public async Task<AppUserDto?> GetByAzureAdObjectIdAsync(string azureAdObjectId)
        {
            _logger.LogInformation("GetByAzureAdObjectIdAsync called with AzureAdObjectId: {AzureAdObjectId}", azureAdObjectId);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var appUser = await context.AppUsers
                .Include(u => u.Location)
                .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAdObjectId);

            if (appUser == null)
            {
                _logger.LogWarning("User with AzureAdObjectId: {AzureAdObjectId} not found.", azureAdObjectId);
                return null;
            }

            var userDto = new AppUserDto
            {
                Id = appUser.Id,
                Name = appUser.Name,
                IsActive = appUser.IsActive,
                AzureAdObjectId = appUser.AzureAdObjectId,
                LocationId = appUser.LocationId,
                LocationName = appUser.Location != null ? appUser.Location.Name : string.Empty
            };
            _logger.LogInformation("GetByAzureAdObjectIdAsync completed, returning user: {UserName}", userDto.Name);
            return userDto;
        }

        /// <inheritdoc />
        public async Task<AppUserDto> CreateOrUpdateUserFromAzureAdAsync(ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("CreateOrUpdateUserFromAzureAdAsync called for user: {UserPrincipalName}", userPrincipal?.Identity?.Name);
            if (userPrincipal == null)
            {
                _logger.LogError("userPrincipal cannot be null.");
                throw new ArgumentNullException(nameof(userPrincipal));
            }

            var azureAdObjectId = userPrincipal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (string.IsNullOrEmpty(azureAdObjectId))
            {
                _logger.LogError("Azure AD Object ID not found in claims.");
                throw new ArgumentNullException(nameof(azureAdObjectId), "Azure AD Object ID not found in claims.");
            }

            var name = userPrincipal.Identity?.Name ?? "Unknown User";
            _logger.LogInformation("Processing user with AzureAdObjectId: {AzureAdObjectId} and Name: {Name}", azureAdObjectId, name);

            await using var context = await _contextFactory.CreateDbContextAsync();
            AppUser appUser;

            try
            {
                appUser = await context.AppUsers
                    .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAdObjectId);

                if (appUser == null)
                {
                    _logger.LogInformation("User with AzureAdObjectId: {AzureAdObjectId} not found. Creating new user.", azureAdObjectId);
                    appUser = new AppUser
                    {
                        AzureAdObjectId = azureAdObjectId,
                        Name = name,
                        IsActive = true,
                        LocationId = 1
                    };
                    context.AppUsers.Add(appUser);
                    await context.SaveChangesAsync();
                    _logger.LogInformation("New user created with Id: {UserId}", appUser.Id);
                }
                else
                {
                    _logger.LogInformation("User with AzureAdObjectId: {AzureAdObjectId} found with Id: {UserId}. Verifying if update is needed.", azureAdObjectId, appUser.Id);
                }

                if (appUser.LocationId != 0 && appUser.Location == null)
                {
                    _logger.LogInformation("Loading location for UserId: {UserId}, LocationId: {LocationId}", appUser.Id, appUser.LocationId);
                    appUser.Location = await context.Locations.FindAsync(appUser.LocationId);
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred in CreateOrUpdateUserFromAzureAdAsync for AzureAdObjectId: {AzureAdObjectId}.", azureAdObjectId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in CreateOrUpdateUserFromAzureAdAsync for AzureAdObjectId: {AzureAdObjectId}.", azureAdObjectId);
                throw;
            }

            var resultDto = new AppUserDto
            {
                Id = appUser.Id,
                Name = appUser.Name,
                IsActive = appUser.IsActive,
                AzureAdObjectId = appUser.AzureAdObjectId,
                LocationId = appUser.LocationId,
                LocationName = appUser.Location != null ? appUser.Location.Name : string.Empty
            };
            _logger.LogInformation("CreateOrUpdateUserFromAzureAdAsync completed for UserId: {UserId}, Name: {UserName}", resultDto.Id, resultDto.Name);
            return resultDto;
        }

        /// <inheritdoc />
        public async Task UpdateAppUserAsync(int userId, UpdateAppUserDto userToUpdate)
        {
            _logger.LogInformation("UpdateAppUserAsync called for UserId: {UserId} with Data: IsActive={IsActive}, LocationId={LocationId}",
                userId, userToUpdate.IsActive, userToUpdate.LocationId);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var appUser = await context.AppUsers.FindAsync(userId);

            if (appUser == null)
            {
                _logger.LogError("User with ID {UserId} not found for update.", userId);
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            appUser.IsActive = userToUpdate.IsActive;
            appUser.LocationId = userToUpdate.LocationId;

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated user with ID {UserId}.", userId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId} in the database.", userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task AssignRoleToUserAsync(int userId, int roleId)
        {
            _logger.LogInformation("AssignRoleToUserAsync called for UserId: {UserId}, RoleId: {RoleId}", userId, roleId);
            await using var context = await _contextFactory.CreateDbContextAsync();

            var userExists = await context.AppUsers.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("User with Id: {UserId} not found.", userId);
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var roleExists = await context.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
            {
                _logger.LogWarning("Role with Id: {RoleId} not found.", roleId);
                throw new KeyNotFoundException($"Role with ID {roleId} not found.");
            }

            var existingAssignment = await context.UserRoles
                .AnyAsync(ur => ur.AppUserId == userId && ur.RoleId == roleId);

            if (existingAssignment)
            {
                _logger.LogInformation("Role {RoleId} is already assigned to User {UserId}.", roleId, userId);
                return;
            }

            context.UserRoles.Add(new UserRole { AppUserId = userId, RoleId = roleId });

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Role {RoleId} assigned to User {UserId} successfully.", roleId, userId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while assigning Role {RoleId} to User {UserId}.", roleId, userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RemoveRoleFromUserAsync(int userId, int roleId)
        {
            _logger.LogInformation("RemoveRoleFromUserAsync called for UserId: {UserId}, RoleId: {RoleId}", userId, roleId);
            await using var context = await _contextFactory.CreateDbContextAsync();

            var assignment = await context.UserRoles
                .FirstOrDefaultAsync(ur => ur.AppUserId == userId && ur.RoleId == roleId);

            if (assignment == null)
            {
                _logger.LogWarning("Role {RoleId} is not assigned to User {UserId}. Cannot remove.", roleId, userId);
                throw new KeyNotFoundException($"Role {roleId} not assigned to user {userId}.");
            }

            context.UserRoles.Remove(assignment);

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Role {RoleId} removed from User {UserId} successfully.", roleId, userId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while removing Role {RoleId} from User {UserId}.", roleId, userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdateUserRolesAsync(int userId, List<int> newRoleIds)
        {
            _logger.LogInformation("UpdateUserRolesAsync called for UserId: {UserId} with {RoleCount} new role IDs.", userId, newRoleIds.Count);
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.AppUsers
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User with Id: {UserId} not found for role update.", userId);
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();

            var roleIdsToAdd = newRoleIds.Except(currentRoleIds).ToList();
            if (roleIdsToAdd.Any())
            {
                _logger.LogInformation("Adding roles {RoleIds} to user {UserId}", string.Join(", ", roleIdsToAdd), userId);
                var rolesToAdd = roleIdsToAdd.Select(roleId => new UserRole { AppUserId = userId, RoleId = roleId });
                await context.UserRoles.AddRangeAsync(rolesToAdd);
            }

            var roleIdsToRemove = currentRoleIds.Except(newRoleIds).ToList();
            if (roleIdsToRemove.Any())
            {
                _logger.LogInformation("Removing roles {RoleIds} from user {UserId}", string.Join(", ", roleIdsToRemove), userId);
                var rolesToRemove = user.UserRoles.Where(ur => roleIdsToRemove.Contains(ur.RoleId));
                context.UserRoles.RemoveRange(rolesToRemove);
            }

            if (!roleIdsToAdd.Any() && !roleIdsToRemove.Any())
            {
                _logger.LogInformation("No role changes detected for user {UserId}.", userId);
                return;
            }

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully synchronized roles for user {UserId}.", userId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while synchronizing roles for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RoleDto>> GetRolesForUserAsync(int userId)
        {
            _logger.LogInformation("GetRolesForUserAsync called for UserId: {UserId}", userId);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.AppUsers
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User with Id: {UserId} not found.", userId);
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var roles = user.UserRoles
                .Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Permissions = ur.Role.RolePermissions.Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name
                    }).ToList()
                })
                .OrderBy(r => r.Name)
                .ToList();

            _logger.LogInformation("GetRolesForUserAsync completed for UserId: {UserId}, returning {RoleCount} roles.", userId, roles.Count);
            return roles;
        }

        /// <inheritdoc />
        public async Task<bool> UserHasPermissionAsync(int userId, string permissionName)
        {
            _logger.LogInformation("UserHasPermissionAsync called for UserId: {UserId}, PermissionName: {PermissionName}", userId, permissionName);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.AppUsers
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User with Id: {UserId} not found for permission check.", userId);
                return false;
            }

            var hasPermission = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Any(rp => rp.Permission.Name == permissionName);

            _logger.LogInformation("UserHasPermissionAsync check for UserId: {UserId}, PermissionName: {PermissionName} resulted in {HasPermission}.", userId, permissionName, hasPermission);
            return hasPermission;
        }

        /// <inheritdoc />
        public async Task<bool> UserHasRoleAsync(int userId, string roleName)
        {
            _logger.LogInformation("UserHasRoleAsync called for UserId: {UserId}, RoleName: {RoleName}", userId, roleName);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.AppUsers
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User with Id: {UserId} not found for role check.", userId);
                return false;
            }

            var hasRole = user.UserRoles
                .Any(ur => ur.Role.Name == roleName);

            _logger.LogInformation("UserHasRoleAsync check for UserId: {UserId}, RoleName: {RoleName} resulted in {HasRole}.", userId, roleName, hasRole);
            return hasRole;
        }
    }
}