using UPortal.Dtos;
using System.Security.Claims;

namespace UPortal.Services
{
    /// <summary>
    /// Defines the contract for a service that manages application users and their roles.
    /// </summary>
    public interface IAppUserService
    {
        /// <summary>
        /// Retrieves a list of all application users with their associated data.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="AppUserDto"/>.</returns>
        Task<List<AppUserDto>> GetAllAsync();

        /// <summary>
        /// Retrieves a specific application user by their unique Azure Active Directory Object ID.
        /// </summary>
        /// <param name="azureAdObjectId">The Azure AD Object ID of the user to find.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains the <see cref="AppUserDto"/> if found; otherwise, null.
        /// </returns>
        Task<AppUserDto?> GetByAzureAdObjectIdAsync(string azureAdObjectId);

        /// <summary>
        /// Creates a new user in the local database based on Azure AD claims if they don't already exist.
        /// If the user exists, it may update certain properties based on the claims.
        /// </summary>
        /// <param name="userPrincipal">The <see cref="ClaimsPrincipal"/> for the authenticated user from Azure AD.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the created or retrieved <see cref="AppUserDto"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if userPrincipal or its Azure AD Object ID claim is null or empty.</exception>
        Task<AppUserDto> CreateOrUpdateUserFromAzureAdAsync(ClaimsPrincipal userPrincipal);

        /// <summary>
        /// Updates the administrative details of an existing application user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to update.</param>
        /// <param name="userToUpdate">A DTO containing the updated information for the user.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if a user with the specified ID is not found.</exception>
        Task UpdateAppUserAsync(int userId, UpdateAppUserDto userToUpdate);

        /// <summary>
        /// Assigns a specific role to a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="roleId">The unique identifier of the role to assign.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the user or role is not found.</exception>
        Task AssignRoleToUserAsync(int userId, int roleId);

        /// <summary>
        /// Removes a specific role from a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="roleId">The unique identifier of the role to remove.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the user, role, or assignment is not found.</exception>
        Task RemoveRoleFromUserAsync(int userId, int roleId);

        /// <summary>
        /// Updates all roles for a specific user based on a provided list of role IDs.
        /// This method synchronizes the user's roles, adding new ones and removing those not in the list.
        /// </summary>
        /// <param name="userId">The ID of the user to update.</param>
        /// <param name="newRoleIds">A list of role IDs that the user should have after the update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if a user with the specified ID is not found.</exception>
        Task UpdateUserRolesAsync(int userId, List<int> newRoleIds);

        /// <summary>
        /// Retrieves all roles assigned to a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains an enumerable of <see cref="RoleDto"/> for the user.
        /// </returns>
        /// <exception cref="KeyNotFoundException">Thrown if a user with the specified ID is not found.</exception>
        Task<IEnumerable<RoleDto>> GetRolesForUserAsync(int userId);

        /// <summary>
        /// Checks if a user has a specific permission, granted through any of their assigned roles.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="permissionName">The name of the permission to check.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result is true if the user has the permission; otherwise, false.
        /// </returns>
        Task<bool> UserHasPermissionAsync(int userId, string permissionName);

        /// <summary>
        /// Checks if a user is assigned a specific role.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="roleName">The name of the role to check.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result is true if the user has the role; otherwise, false.
        /// </returns>
        Task<bool> UserHasRoleAsync(int userId, string roleName);
    }
}