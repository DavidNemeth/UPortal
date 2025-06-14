using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UPortal.Data;
using UPortal.Data.Models;
using UPortal.Dtos;

namespace UPortal.Services
{
    public class RoleService : IRoleService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<RoleService> _logger;

        public RoleService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<RoleService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            _logger.LogInformation("GetAllRolesAsync called");
            await using var context = await _contextFactory.CreateDbContextAsync();
            var roles = await context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Permissions = r.RolePermissions.Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name
                    }).ToList()
                })
                .OrderBy(r => r.Name)
                .ToListAsync();
            _logger.LogInformation("GetAllRolesAsync completed, returning {RoleCount} roles.", roles.Count);
            return roles;
        }

        public async Task<RoleDto> GetRoleByIdAsync(int id)
        {
            _logger.LogInformation("GetRoleByIdAsync called with Id: {Id}", id);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var role = await context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Where(r => r.Id == id)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Permissions = r.RolePermissions.Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (role == null)
            {
                _logger.LogWarning("Role with Id: {Id} not found.", id);
                return null;
            }

            _logger.LogInformation("GetRoleByIdAsync completed, returning role: {RoleName}", role.Name);
            return role;
        }

        public async Task<RoleDto> CreateRoleAsync(CreateRoleDto roleDto)
        {
            _logger.LogInformation("CreateRoleAsync called for role: {RoleName}", roleDto.Name);
            if (roleDto == null)
            {
                _logger.LogError("roleDto cannot be null.");
                throw new ArgumentNullException(nameof(roleDto));
            }

            await using var context = await _contextFactory.CreateDbContextAsync();

            var newRole = new Role
            {
                Name = roleDto.Name
            };

            // Add permissions if any are specified
            if (roleDto.PermissionIds != null && roleDto.PermissionIds.Any())
            {
                foreach (var permissionId in roleDto.PermissionIds)
                {
                    var permission = await context.Permissions.FindAsync(permissionId);
                    if (permission != null)
                    {
                        newRole.RolePermissions.Add(new RolePermission { Permission = permission });
                    }
                    else
                    {
                        _logger.LogWarning("Permission with Id: {PermissionId} not found while creating role {RoleName}.", permissionId, roleDto.Name);
                        // Optionally, throw an exception or handle as per requirements
                    }
                }
            }

            context.Roles.Add(newRole);

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Role {RoleName} created successfully with Id: {RoleId}.", newRole.Name, newRole.Id);
                return await GetRoleByIdAsync(newRole.Id); // Reuse GetRoleByIdAsync to ensure consistent DTO mapping
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while creating role {RoleName}.", roleDto.Name);
                throw;
            }
        }

        public async Task UpdateRoleAsync(int id, RoleUpdateDto roleDto)
        {
            _logger.LogInformation("UpdateRoleAsync called for role Id: {RoleId}", id);
            if (roleDto == null)
            {
                _logger.LogError("roleDto cannot be null.");
                throw new ArgumentNullException(nameof(roleDto));
            }

            await using var context = await _contextFactory.CreateDbContextAsync();
            var roleToUpdate = await context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (roleToUpdate == null)
            {
                _logger.LogWarning("Role with Id: {RoleId} not found for update.", id);
                throw new KeyNotFoundException($"Role with ID {id} not found.");
            }

            roleToUpdate.Name = roleDto.Name;

            // Update permissions
            // First, remove existing permissions
            roleToUpdate.RolePermissions.Clear();

            // Then, add new permissions if any are specified
            if (roleDto.PermissionIds != null && roleDto.PermissionIds.Any())
            {
                foreach (var permissionId in roleDto.PermissionIds)
                {
                    var permission = await context.Permissions.FindAsync(permissionId);
                    if (permission != null)
                    {
                        roleToUpdate.RolePermissions.Add(new RolePermission { PermissionId = permission.Id });
                    }
                    else
                    {
                        _logger.LogWarning("Permission with Id: {PermissionId} not found while updating role {RoleName}.", permissionId, roleDto.Name);
                        // Optionally, throw an exception or handle as per requirements
                    }
                }
            }

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Role with Id: {RoleId} updated successfully.", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating role Id: {RoleId}.", id);
                throw;
            }
        }

        public async Task DeleteRoleAsync(int id)
        {
            _logger.LogInformation("DeleteRoleAsync called for role Id: {RoleId}", id);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var roleToDelete = await context.Roles.FindAsync(id);

            if (roleToDelete == null)
            {
                _logger.LogWarning("Role with Id: {RoleId} not found for deletion.", id);
                throw new KeyNotFoundException($"Role with ID {id} not found.");
            }

            context.Roles.Remove(roleToDelete); // EF Core will handle cascading deletes for RolePermissions and UserRoles if configured, otherwise manual cleanup is needed.
                                                // Assuming cascade delete is set up for join tables or handled by DB.

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Role with Id: {RoleId} deleted successfully.", id);
            }
            catch (DbUpdateException ex)
            {
                // Handle potential issues, e.g., if the role is still in use and FK constraints prevent deletion
                _logger.LogError(ex, "Database error occurred while deleting role Id: {RoleId}.", id);
                throw;
            }
        }

        public async Task AssignPermissionToRoleAsync(int roleId, int permissionId)
        {
            _logger.LogInformation("AssignPermissionToRoleAsync called for RoleId: {RoleId}, PermissionId: {PermissionId}", roleId, permissionId);
            await using var context = await _contextFactory.CreateDbContextAsync();

            var roleExists = await context.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
            {
                _logger.LogWarning("Role with Id: {RoleId} not found.", roleId);
                throw new KeyNotFoundException($"Role with ID {roleId} not found.");
            }

            var permissionExists = await context.Permissions.AnyAsync(p => p.Id == permissionId);
            if (!permissionExists)
            {
                _logger.LogWarning("Permission with Id: {PermissionId} not found.", permissionId);
                throw new KeyNotFoundException($"Permission with ID {permissionId} not found.");
            }

            var existingAssignment = await context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (existingAssignment)
            {
                _logger.LogInformation("Permission {PermissionId} is already assigned to Role {RoleId}.", permissionId, roleId);
                return; // Already assigned
            }

            context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Permission {PermissionId} assigned to Role {RoleId} successfully.", permissionId, roleId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while assigning Permission {PermissionId} to Role {RoleId}.", permissionId, roleId);
                throw;
            }
        }

        public async Task RemovePermissionFromRoleAsync(int roleId, int permissionId)
        {
            _logger.LogInformation("RemovePermissionFromRoleAsync called for RoleId: {RoleId}, PermissionId: {PermissionId}", roleId, permissionId);
            await using var context = await _contextFactory.CreateDbContextAsync();

            var assignment = await context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (assignment == null)
            {
                _logger.LogWarning("Permission {PermissionId} is not assigned to Role {RoleId}. Cannot remove.", permissionId, roleId);
                throw new KeyNotFoundException($"Permission {permissionId} not assigned to role {roleId}.");
            }

            context.RolePermissions.Remove(assignment);

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Permission {PermissionId} removed from Role {RoleId} successfully.", permissionId, roleId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while removing Permission {PermissionId} from Role {RoleId}.", permissionId, roleId);
                throw;
            }
        }

        public async Task<IEnumerable<PermissionDto>> GetPermissionsForRoleAsync(int roleId)
        {
            _logger.LogInformation("GetPermissionsForRoleAsync called for RoleId: {RoleId}", roleId);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var role = await context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                _logger.LogWarning("Role with Id: {RoleId} not found.", roleId);
                throw new KeyNotFoundException($"Role with ID {roleId} not found.");
            }

            var permissions = role.RolePermissions
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name
                })
                .OrderBy(p => p.Name)
                .ToList();

            _logger.LogInformation("GetPermissionsForRoleAsync completed for RoleId: {RoleId}, returning {PermissionCount} permissions.", roleId, permissions.Count);
            return permissions;
        }
    }
}
