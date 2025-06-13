using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UPortal.Data;
using UPortal.Data.Models;
using UPortal.Dtos;
using Microsoft.Extensions.Logging;

namespace UPortal.Services
{
    /// <summary>
    /// Service for managing external applications.
    /// </summary>
    public class ExternalApplicationService : IExternalApplicationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<ExternalApplicationService> _logger;

        public ExternalApplicationService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ExternalApplicationService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all external applications.
        /// </summary>
        /// <returns>A list of <see cref="ExternalApplicationDto"/>.</returns>
        public async Task<List<ExternalApplicationDto>> GetAllAsync()
        {
            _logger.LogInformation("GetAllAsync called");
            try
            {
                // Create a new DbContext instance for this operation.
                await using var context = await _contextFactory.CreateDbContextAsync();
                var applications = await context.ExternalApplications
                    .Select(e => new ExternalApplicationDto // Project to DTO.
                    {
                        Id = e.Id,
                        AppName = e.AppName,
                        AppUrl = e.AppUrl,
                        IconName = e.IconName
                    })
                    .OrderBy(e => e.AppName) // Order by application name.
                    .ToListAsync();
                _logger.LogInformation("GetAllAsync completed, returning {ApplicationCount} applications.", applications.Count);
                return applications;
            }
            catch (Exception ex) // Catch general exceptions that might occur during database interaction.
            {
                _logger.LogError(ex, "Error occurred while getting all external applications.");
                throw; // Re-throw to allow global error handling.
            }
        }

        /// <summary>
        /// Retrieves a specific external application by its ID.
        /// </summary>
        /// <param name="id">The ID of the external application.</param>
        /// <returns>The <see cref="ExternalApplicationDto"/> if found; otherwise, null.</returns>
           public async Task<ExternalApplicationDto?> GetByIdAsync(int id)
           {
               _logger.LogInformation("GetByIdAsync called with Id: {Id}", id);
               try
               {
                   await using var context = await _contextFactory.CreateDbContextAsync();
                   var app = await context.ExternalApplications.FindAsync(id); // Efficiently find by primary key.
                   if (app == null)
                   {
                       _logger.LogWarning("External application with Id: {Id} not found.", id);
                       return null; // Return null if not found.
                   }
                   // Map entity to DTO.
                   var appDto = new ExternalApplicationDto
                   {
                       Id = app.Id,
                       AppName = app.AppName,
                       AppUrl = app.AppUrl,
                       IconName = app.IconName
                   };
                   _logger.LogInformation("GetByIdAsync completed, returning application: {AppName}", appDto.AppName);
                   return appDto;
               }
               catch (Exception ex) // Catch general exceptions.
               {
                   _logger.LogError(ex, "Error occurred while getting external application by Id: {Id}.", id);
                   throw;
               }
           }

        /// <summary>
        /// Adds a new external application to the database.
        /// </summary>
        /// <param name="externalApplicationDto">The DTO containing the details of the application to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddAsync(ExternalApplicationDto externalApplicationDto)
        {
            _logger.LogInformation("AddAsync called for application: {AppName}", externalApplicationDto.AppName);
            await using var context = await _contextFactory.CreateDbContextAsync();
            // Map DTO to entity.
            var externalApplication = new ExternalApplication
            {
                AppName = externalApplicationDto.AppName,
                AppUrl = externalApplicationDto.AppUrl,
                IconName = externalApplicationDto.IconName
            };
            context.ExternalApplications.Add(externalApplication);
            try
            {
                await context.SaveChangesAsync(); // Persist changes to the database.
                _logger.LogInformation("Successfully added application: {AppName} with Id: {Id}", externalApplication.AppName, externalApplication.Id);
            }
            catch (DbUpdateException ex) // Specifically catch database update exceptions.
            {
                _logger.LogError(ex, "Error adding application {AppName} to the database.", externalApplication.AppName);
                throw; // Re-throw for higher-level error handling.
            }
        }

        /// <summary>
        /// Deletes an external application from the database by its ID.
        /// </summary>
        /// <param name="id">The ID of the application to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("DeleteAsync called for Id: {Id}", id);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var externalApplication = await context.ExternalApplications.FindAsync(id); // Find the entity to delete.
            if (externalApplication != null)
            {
                context.ExternalApplications.Remove(externalApplication); // Mark for deletion.
                try
                {
                    await context.SaveChangesAsync(); // Apply deletion to the database.
                    _logger.LogInformation("Successfully deleted application with Id: {Id}", id);
                }
                catch (DbUpdateException ex) // Catch errors during database update.
                {
                    _logger.LogError(ex, "Error deleting application with Id: {Id} from the database.", id);
                    throw;
                }
            }
            else
            {
                // Log if the application to delete was not found.
                _logger.LogWarning("Application with Id: {Id} not found for deletion.", id);
            }
        }
    }
}
