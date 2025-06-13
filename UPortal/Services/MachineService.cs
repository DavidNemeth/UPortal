using Microsoft.EntityFrameworkCore;
using UPortal.Data;
using UPortal.Dtos;
using Microsoft.Extensions.Logging;

namespace UPortal.Services
{
    /// <summary>
    /// Service for managing machines.
    /// </summary>
    public class MachineService : IMachineService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<MachineService> _logger;

        public MachineService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<MachineService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all machines, including their location and assigned user names.
        /// </summary>
        /// <returns>A list of <see cref="MachineDto"/>.</returns>
        public async Task<List<MachineDto>> GetAllAsync()
        {
            _logger.LogInformation("GetAllAsync called");
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var machines = await context.Machines
                    .Select(machine => new MachineDto // Project to DTO.
                    {
                        Id = machine.Id,
                        Name = machine.Name,
                        LocationName = machine.Location.Name, // Access navigation property for Location name.
                        AssignedUserName = machine.AppUser == null ? "Unassigned" : machine.AppUser.Name // Handle unassigned users.
                    })
                    .ToListAsync();
                _logger.LogInformation("GetAllAsync completed, returning {MachineCount} machines.", machines.Count);
                return machines;
            }
            catch (Exception ex) // General exception for database issues.
            {
                _logger.LogError(ex, "Error occurred while getting all machines.");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a specific machine by its ID, including location and assigned user details.
        /// </summary>
        /// <param name="id">The ID of the machine.</param>
        /// <returns>The <see cref="MachineDto"/> if found; otherwise, null.</returns>
        public async Task<MachineDto?> GetByIdAsync(int id)
        {
            _logger.LogInformation("GetByIdAsync called with Id: {Id}", id);
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var machineDto = await context.Machines
                    .Where(m => m.Id == id) // Filter by ID.
                    .Select(machine => new MachineDto // Project to DTO.
                    {
                        Id = machine.Id,
                        Name = machine.Name,
                        LocationName = machine.Location.Name,
                        LocationId = machine.LocationId,
                        AppUserId = machine.AppUserId,
                        AssignedUserName = machine.AppUser == null ? "Unassigned" : machine.AppUser.Name
                    })
                    .FirstOrDefaultAsync();

                if (machineDto == null)
                {
                    _logger.LogWarning("Machine with Id: {Id} not found.", id);
                }
                else
                {
                    _logger.LogInformation("GetByIdAsync completed, returning machine: {MachineName}", machineDto.Name);
                }
                return machineDto;
            }
            catch (Exception ex) // General exception for database issues.
            {
                _logger.LogError(ex, "Error occurred while getting machine by Id: {Id}.", id);
                throw;
            }
        }

        /// <summary>
        /// Creates a new machine.
        /// </summary>
        /// <param name="machineDto">The DTO containing the details of the machine to create.</param>
        /// <returns>The created <see cref="MachineDto"/>, including details fetched after creation.</returns>
        public async Task<MachineDto> CreateAsync(CreateMachineDto machineDto)
        {
            _logger.LogInformation("CreateAsync called for machine: {MachineName}", machineDto.Name);
            await using var context = await _contextFactory.CreateDbContextAsync();

            var newMachine = new Data.Models.Machine // Map DTO to entity.
            {
                Name = machineDto.Name,
                LocationId = machineDto.LocationId,
                AppUserId = machineDto.AppUserId // AppUserId can be null for unassigned machines.
            };

            context.Machines.Add(newMachine);
            try
            {
                await context.SaveChangesAsync(); // Persist to database.
                _logger.LogInformation("Successfully created machine: {MachineName} with Id: {Id}", newMachine.Name, newMachine.Id);
            }
            catch (DbUpdateException ex) // Specific catch for DB update issues.
            {
                _logger.LogError(ex, "Error creating machine {MachineName} in the database.", newMachine.Name);
                throw;
            }

            // Re-query to get a fully populated DTO, including navigation properties like LocationName and AssignedUserName.
            // This ensures the returned DTO is consistent with what GetByIdAsync would return.
            // The GetByIdAsync method already includes logging.
            return (await GetByIdAsync(newMachine.Id))!; // The '!' (null-forgiving operator) is used as we expect the machine to be found immediately after creation.
        }

        /// <summary>
        /// Updates an existing machine.
        /// </summary>
        /// <param name="id">The ID of the machine to update.</param>
        /// <param name="machineDto">The DTO containing the updated machine details.</param>
        /// <returns>True if the update was successful; false if the machine was not found.</returns>
        public async Task<bool> UpdateAsync(int id, CreateMachineDto machineDto)
        {
            _logger.LogInformation("UpdateAsync called for Id: {Id} with new data: Name={NewName}, LocationId={LocationId}, AppUserId={AppUserId}",
                id, machineDto.Name, machineDto.LocationId, machineDto.AppUserId);
            await using var context = await _contextFactory.CreateDbContextAsync();

            var machineToUpdate = await context.Machines.FindAsync(id); // Find by primary key.
            if (machineToUpdate is null)
            {
                _logger.LogWarning("Machine with Id: {Id} not found for update.", id);
                return false; // Indicate not found.
            }

            // Apply changes from DTO to entity.
            machineToUpdate.Name = machineDto.Name;
            machineToUpdate.LocationId = machineDto.LocationId;
            machineToUpdate.AppUserId = machineDto.AppUserId; // AppUserId can be null.

            try
            {
                await context.SaveChangesAsync(); // Persist changes.
                _logger.LogInformation("Successfully updated machine with Id: {Id}", id);
                return true; // Indicate success.
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating machine with Id: {Id} in the database.", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a machine by its ID.
        /// </summary>
        /// <param name="id">The ID of the machine to delete.</param>
        /// <returns>True if the deletion was successful; false if the machine was not found.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("DeleteAsync called for Id: {Id}", id);
            await using var context = await _contextFactory.CreateDbContextAsync();

            var machineToDelete = await context.Machines.FindAsync(id); // Find entity to delete.
            if (machineToDelete is null)
            {
                _logger.LogWarning("Machine with Id: {Id} not found for deletion.", id);
                return false; // Indicate not found.
            }

            context.Machines.Remove(machineToDelete); // Mark for deletion.
            try
            {
                await context.SaveChangesAsync(); // Apply deletion to database.
                _logger.LogInformation("Successfully deleted machine with Id: {Id}", id);
                return true; // Indicate success.
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting machine with Id: {Id} from the database.", id);
                throw;
            }
        }
    }
}