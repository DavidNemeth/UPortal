using Microsoft.EntityFrameworkCore;
using UPortal.Data.Models;

namespace UPortal.Data
{
    /// <summary>
    /// Provides static methods for seeding initial data into the database.
    /// </summary>
    public class DataSeeder
    {
        /// <summary>
        /// Seeds the database with initial data if it hasn't been seeded already.
        /// This method ensures the database is created and then populates
        /// default locations, machines, permissions, roles, and role-permissions.
        /// </summary>
        /// <param name="app">The <see cref="WebApplication"/> instance to access services.</param>
        /// <returns>A task that represents the asynchronous seed operation.</returns>
        public static async Task SeedAsync(WebApplication app)
        {
            // Create a new scope to resolve services.
            var scope = app.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger<DataSeeder>>(); // This line now works correctly.
            var configuration = serviceProvider.GetRequiredService<IConfiguration>(); // For admin user config

            // Create a new DbContext instance for this seeding operation.
            await using (var context = await contextFactory.CreateDbContextAsync())
            {
                // Ensure the database is created.
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Database schema verified/created.");

                // --- SEED LOCATIONS ---
                if (!await context.Locations.AnyAsync())
                {
                    logger.LogInformation("Seeding Locations...");
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
                    await context.SaveChangesAsync();
                    logger.LogInformation("Locations seeded.");

                    // --- SEED MACHINES (only if locations were just created) ---
                    if (!await context.Machines.AnyAsync())
                    {
                        logger.LogInformation("Seeding Machines...");
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
                        logger.LogInformation("Machines seeded.");
                    }
                }
                else
                {
                    logger.LogInformation("Locations already exist, skipping Location and Machine seeding.");
                }

                // --- SEED PERMISSIONS ---
                var permissionNames = new List<string>
                {
                    "ManageUsers", "ViewUsers", "EditUsers",
                    "ManageRoles", "ViewRoles", "AssignRoles",
                    "ManagePermissions", "ViewPermissions",
                    "ManageSettings",
                    "AccessAdminPages",
                    "ViewDashboard",
                    "ManageMachines", "ViewMachines",
                    "ManageLocations", "ViewLocations",
                    "ManageExternalApplications", "ViewExternalApplications"
                };

                logger.LogInformation("Seeding Permissions...");
                foreach (var name in permissionNames)
                {
                    if (!await context.Permissions.AnyAsync(p => p.Name == name))
                    {
                        context.Permissions.Add(new Permission { Name = name });
                        logger.LogInformation("Added permission: {PermissionName}", name);
                    }
                }
                await context.SaveChangesAsync(); // Save all new permissions at once
                logger.LogInformation("Permissions seeding complete.");

                // --- SEED "ADMIN" ROLE ---
                logger.LogInformation("Seeding Admin Role...");
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole == null)
                {
                    adminRole = new Role { Name = "Admin" };
                    context.Roles.Add(adminRole);
                    await context.SaveChangesAsync(); // Save to get adminRole.Id
                    logger.LogInformation("Created 'Admin' role with Id: {RoleId}.", adminRole.Id);
                }
                else
                {
                    logger.LogInformation("'Admin' role already exists with Id: {RoleId}.", adminRole.Id);
                }

                // --- ASSIGN ALL PERMISSIONS TO "ADMIN" ROLE ---
                logger.LogInformation("Assigning permissions to Admin Role (Id: {RoleId})...", adminRole.Id);
                var allPermissions = await context.Permissions.ToListAsync();
                int assignedCount = 0;
                foreach (var permission in allPermissions)
                {
                    if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id))
                    {
                        context.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = permission.Id });
                        logger.LogInformation("Assigned permission '{PermissionName}' (Id: {PermissionId}) to 'Admin' role.", permission.Name, permission.Id);
                        assignedCount++;
                    }
                }
                if (assignedCount > 0)
                {
                    await context.SaveChangesAsync();
                }
                logger.LogInformation("Admin role permission assignment complete. {AssignedCount} new permissions assigned.", assignedCount);

                // --- ASSIGN "ADMIN" ROLE TO AN INITIAL ADMIN USER (FROM CONFIGURATION) ---
                var adminUserAzureAdObjectId = configuration["AdminUserAzureAdObjectId"];
                if (!string.IsNullOrEmpty(adminUserAzureAdObjectId))
                {
                    var adminUser = await context.AppUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == adminUserAzureAdObjectId);
                    if (adminUser != null)
                    {
                        logger.LogInformation("Found admin user: {AdminUserName} (Id: {AdminUserId}) by AzureAdObjectId.", adminUser.Name, adminUser.Id);
                        if (!await context.UserRoles.AnyAsync(ur => ur.AppUserId == adminUser.Id && ur.RoleId == adminRole.Id))
                        {
                            context.UserRoles.Add(new UserRole { AppUserId = adminUser.Id, RoleId = adminRole.Id });
                            await context.SaveChangesAsync();
                            logger.LogInformation("Assigned 'Admin' role to user: {AdminUserName}.", adminUser.Name);
                        }
                        else
                        {
                            logger.LogInformation("User {AdminUserName} already has 'Admin' role.", adminUser.Name);
                        }
                    }
                    else
                    {
                        logger.LogWarning("Admin user with AzureAdObjectId '{AdminUserAzureAdObjectId}' not found in AppUsers table. Skipping role assignment. This user might be created upon first login.", adminUserAzureAdObjectId);
                    }
                }
                else
                {
                    logger.LogWarning("AdminUserAzureAdObjectId not configured in appsettings.json. Skipping role assignment to initial admin user.");
                }
                logger.LogInformation("Data seeding for RBAC complete.");
            }
            // Dispose the scope after all operations are complete
            scope.Dispose();
        }
    }
}