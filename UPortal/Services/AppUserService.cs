using Microsoft.EntityFrameworkCore;
using UPortal.Data;
using UPortal.Data.Models;
using UPortal.Dtos;
using System.Security.Claims;
using System.Linq;
using System; // Added for ArgumentNullException
using System.Collections.Generic; // Added for KeyNotFoundException
using Microsoft.Extensions.Logging;

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

        /// <summary>
        /// Retrieves all application users.
        /// </summary>
        /// <returns>A list of <see cref="AppUserDto"/>.</returns>
        public async Task<List<AppUserDto>> GetAllAsync()
        {
            _logger.LogInformation("GetAllAsync called");
            // Create a new DbContext instance for this operation.
            await using var context = await _contextFactory.CreateDbContextAsync();
            var users = await context.AppUsers
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
            _logger.LogInformation("GetAllAsync completed, returning {UserCount} users.", users.Count);
            return users;
        }

        /// <summary>
        /// Retrieves a specific application user by their Azure AD Object ID.
        /// </summary>
        /// <param name="azureAdObjectId">The Azure AD Object ID of the user.</param>
        /// <returns>The <see cref="AppUserDto"/> if found; otherwise, null.</returns>
        public async Task<AppUserDto?> GetByAzureAdObjectIdAsync(string azureAdObjectId)
        {
            _logger.LogInformation("GetByAzureAdObjectIdAsync called with AzureAdObjectId: {AzureAdObjectId}", azureAdObjectId);
            await using var context = await _contextFactory.CreateDbContextAsync();
            // Include location details in the query.
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
                IsAdmin = appUser.IsAdmin,
                IsActive = appUser.IsActive,
                AzureAdObjectId = appUser.AzureAdObjectId,
                LocationId = appUser.LocationId,
                LocationName = appUser.Location != null ? appUser.Location.Name : string.Empty
            };
            _logger.LogInformation("GetByAzureAdObjectIdAsync completed, returning user: {UserName}", userDto.Name);
            return userDto;
        }

        /// <summary>
        /// Creates a new application user or updates an existing one based on Azure AD claims.
        /// If the user does not exist in the local database, a new user is created with default settings.
        /// </summary>
        /// <param name="userPrincipal">The <see cref="ClaimsPrincipal"/> representing the authenticated user from Azure AD.</param>
        /// <returns>The created or updated <see cref="AppUserDto"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="userPrincipal"/> or its Azure AD Object ID claim is null or empty.</exception>
        public async Task<AppUserDto> CreateOrUpdateUserFromAzureAdAsync(ClaimsPrincipal userPrincipal)
        {
            _logger.LogInformation("CreateOrUpdateUserFromAzureAdAsync called for user: {UserPrincipalName}", userPrincipal?.Identity?.Name);
            if (userPrincipal == null)
            {
                _logger.LogError("userPrincipal cannot be null.");
                throw new ArgumentNullException(nameof(userPrincipal)); // Ensure this exception is documented.
            }

            // Retrieve the Azure AD Object ID from the user's claims.
            var azureAdObjectId = userPrincipal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (string.IsNullOrEmpty(azureAdObjectId))
            {
                _logger.LogError("Azure AD Object ID not found in claims.");
                throw new ArgumentNullException(nameof(azureAdObjectId), "Azure AD Object ID not found in claims."); // Ensure this exception is documented.
            }

            var name = userPrincipal.Identity?.Name ?? "Unknown User"; // Default to "Unknown User" if name is not available.
            _logger.LogInformation("Processing user with AzureAdObjectId: {AzureAdObjectId} and Name: {Name}", azureAdObjectId, name);

            await using var context = await _contextFactory.CreateDbContextAsync();
            AppUser appUser;

            try
            {
                appUser = await context.AppUsers
                    .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAdObjectId);

                if (appUser == null)
                {
                    // User does not exist, create a new one.
                    _logger.LogInformation("User with AzureAdObjectId: {AzureAdObjectId} not found. Creating new user.", azureAdObjectId);
                    appUser = new AppUser
                    {
                        AzureAdObjectId = azureAdObjectId,
                        Name = name,
                        IsAdmin = false, // Default for new users, can be changed later by an admin.
                        IsActive = true,   // Default for new users.
                        LocationId = 1    // Placeholder default LocationId, ensure this is a valid existing ID or handle it.
                    };
                    context.AppUsers.Add(appUser);
                    await context.SaveChangesAsync(); // Persist the new user to the database.
                    _logger.LogInformation("New user created with Id: {UserId}", appUser.Id);
                }
                else
                {
                    // User exists, log information. Optionally, update user properties like name if they differ from Azure AD.
                    _logger.LogInformation("User with AzureAdObjectId: {AzureAdObjectId} found with Id: {UserId}. Verifying if update is needed.", azureAdObjectId, appUser.Id);
                    // Example: Update name if it has changed in Azure AD.
                    // if (appUser.Name != name && !string.IsNullOrEmpty(name))
                    // {
                    //     appUser.Name = name;
                    //     await context.SaveChangesAsync();
                    //     _logger.LogInformation("Updated user name for UserId: {UserId} to {NewName}", appUser.Id, name);
                    // }
                }

                // Ensure Location navigation property is loaded if LocationId is valid and Location is null.
                // This is important if the appUser object is used further where appUser.Location is accessed.
                if (appUser.LocationId != 0 && appUser.Location == null)
                {
                    _logger.LogInformation("Loading location for UserId: {UserId}, LocationId: {LocationId}", appUser.Id, appUser.LocationId);
                    appUser.Location = await context.Locations.FindAsync(appUser.LocationId);
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred in CreateOrUpdateUserFromAzureAdAsync for AzureAdObjectId: {AzureAdObjectId}.", azureAdObjectId);
                throw; // Re-throw to allow global error handling or further upstack handling.
            }
            catch (Exception ex) // Catch other potential exceptions to ensure logging.
            {
                _logger.LogError(ex, "An unexpected error occurred in CreateOrUpdateUserFromAzureAdAsync for AzureAdObjectId: {AzureAdObjectId}.", azureAdObjectId);
                throw; // Re-throw.
            }

            // Map the AppUser entity to AppUserDto for returning to the caller.
            var resultDto = new AppUserDto
            {
                Id = appUser.Id,
                Name = appUser.Name,
                IsAdmin = appUser.IsAdmin,
                IsActive = appUser.IsActive,
                AzureAdObjectId = appUser.AzureAdObjectId,
                LocationId = appUser.LocationId,
                LocationName = appUser.Location != null ? appUser.Location.Name : string.Empty
            };
            _logger.LogInformation("CreateOrUpdateUserFromAzureAdAsync completed for UserId: {UserId}, Name: {UserName}", resultDto.Id, resultDto.Name);
            return resultDto;
        }

        /// <summary>
        /// Updates an existing application user's details.
        /// </summary>
        /// <param name="userId">The ID of the user to update.</param>
        /// <param name="userToUpdate">An <see cref="UpdateAppUserDto"/> containing the updated user information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if a user with the specified <paramref name="userId"/> is not found.</exception>
        public async Task UpdateAppUserAsync(int userId, UpdateAppUserDto userToUpdate)
        {
            _logger.LogInformation("UpdateAppUserAsync called for UserId: {UserId} with Data: IsActive={IsActive}, IsAdmin={IsAdmin}, LocationId={LocationId}",
                userId, userToUpdate.IsActive, userToUpdate.IsAdmin, userToUpdate.LocationId);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var appUser = await context.AppUsers.FindAsync(userId); // Retrieve the user to be updated.

            if (appUser == null)
            {
                _logger.LogError("User with ID {UserId} not found for update.", userId);
                throw new KeyNotFoundException($"User with ID {userId} not found."); // Ensure this exception is documented.
            }

            // Apply updates from the DTO to the entity.
            appUser.IsActive = userToUpdate.IsActive;
            appUser.IsAdmin = userToUpdate.IsAdmin;
            appUser.LocationId = userToUpdate.LocationId; // Update LocationId

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated user with ID {UserId}.", userId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId} in the database.", userId);
                throw; // Re-throw the exception after logging
            }
        }
    }
}