using Microsoft.EntityFrameworkCore;
using UPortal.Data;
using UPortal.Dtos;
using UPortal.Services;

namespace UPortal.Services
{
    public class LocationService : ILocationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        // The service now only depends on the DbContextFactory.
        public LocationService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<LocationDto>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Manually project the Location entity to a LocationDto.
            // This is still highly efficient and executes as a single SQL query.
            return await context.Locations
                .Select(location => new LocationDto
                {
                    Id = location.Id,
                    Name = location.Name,
                    UserCount = location.AppUsers.Count(),
                    MachineCount = location.Machines.Count()
                })
                .ToListAsync();
        }

        public async Task<LocationDto?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Find the location and project it to a DTO in the same query.
            var locationDto = await context.Locations
                .Where(l => l.Id == id)
                .Select(l => new LocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    UserCount = l.AppUsers.Count(),
                    MachineCount = l.Machines.Count()
                })
                .FirstOrDefaultAsync();

            return locationDto;
        }

        public async Task<LocationDto> CreateAsync(CreateLocationDto locationDto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Manually map from the DTO to the entity.
            var newLocation = new Location
            {
                Name = locationDto.Name
            };

            context.Locations.Add(newLocation);
            await context.SaveChangesAsync();

            // Manually map the newly created entity back to a DTO to return.
            return new LocationDto
            {
                Id = newLocation.Id,
                Name = newLocation.Name
            };
        }

        public async Task<bool> UpdateAsync(int id, CreateLocationDto locationDto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var locationToUpdate = await context.Locations.FindAsync(id);

            if (locationToUpdate is null)
            {
                return false;
            }

            locationToUpdate.Name = locationDto.Name;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var locationToDelete = await context.Locations.FindAsync(id);

            if (locationToDelete is null)
            {
                return false;
            }

            context.Locations.Remove(locationToDelete);
            await context.SaveChangesAsync();
            return true;
        }
    }
}