using Microsoft.EntityFrameworkCore;
using UPortal.Data;
using UPortal.Dtos;

namespace UPortal.Services
{
    public class MachineService : IMachineService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public MachineService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<MachineDto>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Machines
                .Select(machine => new MachineDto
                {
                    Id = machine.Id,
                    Name = machine.Name,
                    LocationName = machine.Location.Name, // EF translates this navigation property access
                    // Use a ternary operator to handle potentially unassigned users
                    AssignedUserName = machine.AppUser == null ? "Unassigned" : machine.AppUser.Name
                })
                .ToListAsync();
        }

        public async Task<MachineDto?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Machines
                .Where(m => m.Id == id)
                .Select(machine => new MachineDto
                {
                    Id = machine.Id,
                    Name = machine.Name,
                    LocationName = machine.Location.Name,
                    LocationId = machine.LocationId,
                    AppUserId = machine.AppUserId,
                    AssignedUserName = machine.AppUser == null ? "Unassigned" : machine.AppUser.Name
                })
                .FirstOrDefaultAsync();
        }

        public async Task<MachineDto> CreateAsync(CreateMachineDto machineDto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var newMachine = new Data.Models.Machine
            {
                Name = machineDto.Name,
                LocationId = machineDto.LocationId,
                AppUserId = machineDto.AppUserId
            };

            context.Machines.Add(newMachine);
            await context.SaveChangesAsync();

            // To ensure we return a fully populated DTO, we can re-query using our GetByIdAsync method.
            // This avoids duplicating mapping logic and ensures consistency.
            return (await GetByIdAsync(newMachine.Id))!;
        }

        public async Task<bool> UpdateAsync(int id, CreateMachineDto machineDto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var machineToUpdate = await context.Machines.FindAsync(id);
            if (machineToUpdate is null)
            {
                return false;
            }

            machineToUpdate.Name = machineDto.Name;
            machineToUpdate.LocationId = machineDto.LocationId;
            machineToUpdate.AppUserId = machineDto.AppUserId;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var machineToDelete = await context.Machines.FindAsync(id);
            if (machineToDelete is null)
            {
                return false;
            }

            context.Machines.Remove(machineToDelete);
            await context.SaveChangesAsync();
            return true;
        }
    }
}