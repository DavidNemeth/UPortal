using Microsoft.EntityFrameworkCore;
using UPortal.Data.Models;

namespace UPortal.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(WebApplication app)
        {
            // The service provider is needed to get the DbContextFactory
            var serviceProvider = app.Services.CreateScope().ServiceProvider;
            var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

            await using (var context = await contextFactory.CreateDbContextAsync())
            {
                // We ensure the database is created.
                // In a production environment, you might rely solely on migrations.
                await context.Database.EnsureCreatedAsync();

                // --- SEED LOCATIONS ---
                // We check if any locations already exist to avoid duplicating data.
                if (!await context.Locations.AnyAsync())
                {
                    var locations = new List<Location>
                    {
                        new Location { Name = "Pitten" },
                        new Location { Name = "Trostberg" },
                        new Location { Name = "Dunaújváros" },
                        new Location { Name = "Spremberg" },
                        new Location { Name = "Corlu" },
                        new Location { Name = "Denizli" },
                        new Location { Name = "Gelsenkirchen" }
                    };
                    await context.Locations.AddRangeAsync(locations);
                    await context.SaveChangesAsync(); // Save locations to get their IDs

                    // --- SEED MACHINES ---
                    // This logic now runs only if locations were just created.
                    if (!await context.Machines.AnyAsync())
                    {
                        var machines = new List<Machine>();
                        foreach (var location in locations)
                        {
                            for (int i = 1; i <= 3; i++)
                            {
                                machines.Add(new Machine
                                {
                                    Name = $"PM{i}",
                                    LocationId = location.Id
                                });
                            }
                        }
                        await context.Machines.AddRangeAsync(machines);
                        await context.SaveChangesAsync();
                    }
                }

                // You can add more complex seeding logic here for the future.
                // For example: "if a machine named 'PM4' doesn't exist in Pitten, add it."
            }
        }
    }
}
