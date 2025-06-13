using Microsoft.EntityFrameworkCore;
using UPortal.Data;
using UPortal.Dtos;
using Microsoft.Extensions.Logging;

namespace UPortal.Services
{
    /// <summary>
    /// Service for managing locations.
    /// </summary>
    public class LocationService : ILocationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<LocationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationService"/> class.
        /// </summary>
        /// <param name="contextFactory">The database context factory.</param>
        /// <param name="logger">The logger.</param>
        public LocationService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<LocationService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all locations along with their user and machine counts.
        /// </summary>
        /// <returns>A list of <see cref="LocationDto"/>.</returns>
        public async Task<List<LocationDto>> GetAllAsync()
        {
            _logger.LogInformation("GetAllAsync called");
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                // Project to LocationDto, including counts of related entities.
                var locations = await context.Locations
                    .Select(location => new LocationDto
                    {
                        Id = location.Id,
                        Name = location.Name,
                        UserCount = location.AppUsers.Count(), // Efficiently counts related AppUsers.
                        MachineCount = location.Machines.Count() // Efficiently counts related Machines.
                    })
                    .ToListAsync();
                _logger.LogInformation("GetAllAsync completed, returning {LocationCount} locations.", locations.Count);
                return locations;
            }
            catch (Exception ex) // General exception catch for database errors.
            {
                _logger.LogError(ex, "Error occurred while getting all locations.");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a specific location by its ID, including user and machine counts.
        /// </summary>
        /// <param name="id">The ID of the location.</param>
        /// <returns>The <see cref="LocationDto"/> if found; otherwise, null.</returns>
        public async Task<LocationDto?> GetByIdAsync(int id)
        {
            _logger.LogInformation("GetByIdAsync called with Id: {Id}", id);
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var locationDto = await context.Locations
                    .Where(l => l.Id == id) // Filter by ID.
                    .Select(l => new LocationDto // Project to DTO.
                    {
                        Id = l.Id,
                        Name = l.Name,
                        UserCount = l.AppUsers.Count(),
                        MachineCount = l.Machines.Count()
                    })
                    .FirstOrDefaultAsync();

                if (locationDto == null)
                {
                    _logger.LogWarning("Location with Id: {Id} not found.", id);
                }
                else
                {
                    _logger.LogInformation("GetByIdAsync completed, returning location: {LocationName}", locationDto.Name);
                }
                return locationDto;
            }
            catch (Exception ex) // General exception catch.
            {
                _logger.LogError(ex, "Error occurred while getting location by Id: {Id}.", id);
                throw;
            }
        }

        /// <summary>
        /// Creates a new location.
        /// </summary>
        /// <param name="locationDto">The DTO containing the details of the location to create.</param>
        /// <returns>The created <see cref="LocationDto"/>.</returns>
        public async Task<LocationDto> CreateAsync(CreateLocationDto locationDto)
        {
            _logger.LogInformation("CreateAsync called for location: {LocationName}", locationDto.Name);
            await using var context = await _contextFactory.CreateDbContextAsync();

            var newLocation = new Data.Models.Location
            {
                Name = locationDto.Name // Map DTO to entity.
            };

            context.Locations.Add(newLocation);
            try
            {
                await context.SaveChangesAsync(); // Persist to database.
                _logger.LogInformation("Successfully created location: {LocationName} with Id: {Id}", newLocation.Name, newLocation.Id);
            }
            catch (DbUpdateException ex) // Specific catch for DB update issues.
            {
                _logger.LogError(ex, "Error creating location {LocationName} in the database.", newLocation.Name);
                throw;
            }

            // Return the newly created location as a DTO.
            return new LocationDto
            {
                Id = newLocation.Id,
                Name = newLocation.Name
            };
        }

        /// <summary>
        /// Updates an existing location.
        /// </summary>
        /// <param name="id">The ID of the location to update.</param>
        /// <param name="locationDto">The DTO containing the updated location details.</param>
        /// <returns>True if the update was successful; false if the location was not found.</returns>
        public async Task<bool> UpdateAsync(int id, CreateLocationDto locationDto)
        {
            _logger.LogInformation("UpdateAsync called for Id: {Id} with new name: {NewName}", id, locationDto.Name);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var locationToUpdate = await context.Locations.FindAsync(id); // Find by primary key.

            if (locationToUpdate is null)
            {
                _logger.LogWarning("Location with Id: {Id} not found for update.", id);
                return false; // Indicate not found.
            }

            locationToUpdate.Name = locationDto.Name; // Apply changes.
            try
            {
                await context.SaveChangesAsync(); // Persist changes.
                _logger.LogInformation("Successfully updated location with Id: {Id}", id);
                return true; // Indicate success.
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating location with Id: {Id} in the database.", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a location by its ID.
        /// </summary>
        /// <param name="id">The ID of the location to delete.</param>
        /// <returns>True if the deletion was successful; false if the location was not found.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("DeleteAsync called for Id: {Id}", id);
            await using var context = await _contextFactory.CreateDbContextAsync();
            var locationToDelete = await context.Locations.FindAsync(id); // Find entity to delete.

            if (locationToDelete is null)
            {
                _logger.LogWarning("Location with Id: {Id} not found for deletion.", id);
                return false; // Indicate not found.
            }

            context.Locations.Remove(locationToDelete); // Mark for deletion.
            try
            {
                await context.SaveChangesAsync(); // Apply deletion to database.
                _logger.LogInformation("Successfully deleted location with Id: {Id}", id);
                return true; // Indicate success.
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting location with Id: {Id} from the database.", id);
                throw;
            }
        }
    }
}