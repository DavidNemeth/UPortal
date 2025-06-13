using Microsoft.EntityFrameworkCore;
using UPortal.Data.Models;

namespace UPortal.Data
{
    /// <summary>
    /// Provides static methods for seeding initial data into the database.
    /// </summary>
    public static class DataSeeder
    {
        /// <summary>
        /// Seeds the database with initial data if it hasn't been seeded already.
        /// This method ensures the database is created and then populates
        /// default locations and machines if they don't exist.
        /// </summary>
        /// <param name="app">The <see cref="WebApplication"/> instance to access services.</param>
        /// <returns>A task that represents the asynchronous seed operation.</returns>
        public static async Task SeedAsync(WebApplication app)
        {
            // Create a new scope to resolve services, particularly the DbContextFactory.
            // This is necessary because SeedAsync is static and might be called at a point
            // where scoped services aren't directly available.
            var serviceProvider = app.Services.CreateScope().ServiceProvider;
            var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

            // Create a new DbContext instance for this seeding operation.
            // Using 'await using' ensures the context is properly disposed of.
            await using (var context = await contextFactory.CreateDbContextAsync())
            {
                // Ensure the database is created.
                // For production, migrations are typically preferred over EnsureCreatedAsync for schema management.
                // However, for development or initial setup, EnsureCreatedAsync can be useful.
                await context.Database.EnsureCreatedAsync();

                // --- SEED LOCATIONS ---
                // Check if any locations already exist to prevent duplicating data on subsequent runs.
                if (!await context.Locations.AnyAsync())
                {
                    // Define a list of initial locations.
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
                    // Add the locations to the context.
                    await context.Locations.AddRangeAsync(locations);
                    // Save changes to the database to generate IDs for the new locations.
                    await context.SaveChangesAsync();

                    // --- SEED MACHINES ---
                    // Seed machines only if locations were just created and no machines exist yet.
                    // This prevents adding duplicate machines if the seeder runs multiple times
                    // and locations already existed but machines didn't.
                    if (!await context.Machines.AnyAsync())
                    {
                        var machines = new List<Machine>();
                        // For each newly created location, add some default machines.
                        foreach (var location in locations)
                        {
                            for (int i = 1; i <= 3; i++) // Add 3 machines per location.
                            {
                                machines.Add(new Machine
                                {
                                    Name = $"PM{i}", // Example machine name like "PM1", "PM2".
                                    LocationId = location.Id // Assign to the current location.
                                });
                            }
                        }
                        // Add the machines to the context.
                        await context.Machines.AddRangeAsync(machines);
                        // Save machine data to the database.
                        await context.SaveChangesAsync();
                    }
                }

                // Further seeding logic can be added here.
                // For example, to ensure specific users or other entities exist.
                // Consider more sophisticated checks if data might be partially seeded or modified by users.
            }
        }
    }
}
