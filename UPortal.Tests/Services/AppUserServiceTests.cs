using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UPortal.Data;
using UPortal.Data.Models;
using UPortal.Dtos;
using UPortal.Services;

namespace UPortal.Tests.Services
{
    [TestClass]
    public class AppUserServiceTests
    {
        private DbContextOptions<ApplicationDbContext> _options;
        private Mock<IDbContextFactory<ApplicationDbContext>> _mockDbContextFactory;

        [TestInitialize]
        public void Initialize()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB for each test
                .Options;

            // Ensure a clean context for each test by creating a new one
            var dbContext = new ApplicationDbContext(_options);
            dbContext.Database.EnsureCreated(); // Ensure schema is created

            _mockDbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            _mockDbContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(() => new ApplicationDbContext(_options)); // Return new instance each time
        }

        private ApplicationDbContext CreateContext() => new ApplicationDbContext(_options);

        [TestMethod]
        public async Task GetByAzureAdObjectIdAsync_UserExists_ReturnsUserDto()
        {
            // Arrange
            var azureId = "testAzureId1";
            using (var context = CreateContext())
            {
                context.AppUsers.Add(new AppUser { Name = "Test User 1", AzureAdObjectId = azureId, IsAdmin = true, IsActive = true, LocationId = 1 });
                await context.SaveChangesAsync();
            }
            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            var result = await service.GetByAzureAdObjectIdAsync(azureId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test User 1", result.Name);
            Assert.AreEqual(azureId, result.AzureAdObjectId);
            Assert.IsTrue(result.IsAdmin);
            Assert.IsTrue(result.IsActive);
        }

        [TestMethod]
        public async Task GetByAzureAdObjectIdAsync_UserDoesNotExist_ReturnsNull()
        {
            // Arrange
            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            var result = await service.GetByAzureAdObjectIdAsync("nonExistentId");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_NewUser_CreatesAndReturnsUserDto()
        {
            // Arrange
            var azureId = "newAzureUser";
            var userName = "New User Name";
            var claims = new List<Claim>
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", azureId),
                new Claim(ClaimTypes.Name, userName)
            };
            var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(claimsIdentity);

            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            var resultDto = await service.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);

            // Assert
            Assert.IsNotNull(resultDto);
            Assert.AreEqual(userName, resultDto.Name);
            Assert.AreEqual(azureId, resultDto.AzureAdObjectId);
            Assert.IsFalse(resultDto.IsAdmin); // Default
            Assert.IsTrue(resultDto.IsActive);  // Default

            using (var context = CreateContext())
            {
                var dbUser = await context.AppUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == azureId);
                Assert.IsNotNull(dbUser);
                Assert.AreEqual(userName, dbUser.Name);
                Assert.AreEqual(1, dbUser.LocationId); // Default
            }
        }

        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_ExistingUser_ReturnsExistingUserDtoAndDoesNotAlterSensitiveFields()
        {
            // Arrange
            var azureId = "existingAzureUser";
            var originalName = "Original Name";
            var updatedNameFromClaims = "Updated Name From Claims"; // Name might be updated by claims

            using (var context = CreateContext())
            {
                context.AppUsers.Add(new AppUser {
                    AzureAdObjectId = azureId,
                    Name = originalName,
                    IsAdmin = true,
                    IsActive = false,
                    LocationId = 5
                });
                await context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", azureId),
                new Claim(ClaimTypes.Name, updatedNameFromClaims) // Simulate user having a different name in Azure AD
            };
            var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(claimsIdentity);

            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            var resultDto = await service.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);

            // Assert
            Assert.IsNotNull(resultDto);
            // The current implementation of CreateOrUpdate does NOT update the name if the user exists.
            // It also doesn't update IsAdmin, IsActive, or LocationId from claims.
            Assert.AreEqual(originalName, resultDto.Name);
            Assert.AreEqual(azureId, resultDto.AzureAdObjectId);
            Assert.IsTrue(resultDto.IsAdmin);   // Should remain as originally seeded
            Assert.IsFalse(resultDto.IsActive); // Should remain as originally seeded

            using (var context = CreateContext())
            {
                var dbUser = await context.AppUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == azureId);
                Assert.IsNotNull(dbUser);
                Assert.AreEqual(originalName, dbUser.Name); // Name should not change based on current service logic
                Assert.IsTrue(dbUser.IsAdmin);
                Assert.IsFalse(dbUser.IsActive);
                Assert.AreEqual(5, dbUser.LocationId); // LocationId should not change
                Assert.AreEqual(1, context.AppUsers.Count()); // No new user created
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CreateOrUpdateUserFromAzureAdAsync_NullAzureAdObjectId_ThrowsArgumentNullException()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Test User") }; // No Object ID
            var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(claimsIdentity);
            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            await service.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CreateOrUpdateUserFromAzureAdAsync_EmptyAzureAdObjectId_ThrowsArgumentNullException()
        {
            // Arrange
             var claims = new List<Claim>
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", string.Empty),
                new Claim(ClaimTypes.Name, "Test User")
            };
            var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(claimsIdentity);
            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            await service.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CreateOrUpdateUserFromAzureAdAsync_NullPrincipal_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            await service.CreateOrUpdateUserFromAzureAdAsync(null);
        }

        [TestMethod]
        public async Task UpdateAppUserAsync_UserExists_UpdatesPropertiesIncludingLocationId()
        {
            // Arrange
            var userId = 1;
            var initialLocationId = 10;
            var updatedLocationId = 20;

            using (var context = CreateContext())
            {
                context.Locations.AddRange(
                    new Location { Id = initialLocationId, Name = "Initial Location" },
                    new Location { Id = updatedLocationId, Name = "Updated Location" }
                );
                var user = new AppUser { Id = userId, Name = "Test User", AzureAdObjectId = "updateTest", IsActive = true, IsAdmin = false, LocationId = initialLocationId };
                context.AppUsers.Add(user);
                await context.SaveChangesAsync();
            }
            var service = new AppUserService(_mockDbContextFactory.Object);
            var dto = new UpdateAppUserDto
            {
                IsActive = false,
                IsAdmin = true,
                LocationId = updatedLocationId
            };

            // Act
            await service.UpdateAppUserAsync(userId, dto);

            // Assert
            using (var context = CreateContext())
            {
                var updatedUser = await context.AppUsers.FindAsync(userId);
                Assert.IsNotNull(updatedUser);
                Assert.IsFalse(updatedUser.IsActive);
                Assert.IsTrue(updatedUser.IsAdmin);
                Assert.AreEqual(updatedLocationId, updatedUser.LocationId);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public async Task UpdateAppUserAsync_UserDoesNotExist_ThrowsKeyNotFoundException()
        {
            // Arrange
            var service = new AppUserService(_mockDbContextFactory.Object);
            var dto = new UpdateAppUserDto { IsActive = true, IsAdmin = false, LocationId = 1 };

            // Act
            await service.UpdateAppUserAsync(999, dto); // Non-existent user ID
        }

        [TestMethod]
        public async Task GetAllAsync_ReturnsAllUsersWithCorrectDtoProperties()
        {
            // Arrange
            using (var context = CreateContext())
            {
                context.AppUsers.AddRange(
                    new AppUser { Name = "User A", AzureAdObjectId = "idA", IsAdmin = false, IsActive = true, LocationId = 1 },
                    new AppUser { Name = "User B", AzureAdObjectId = "idB", IsAdmin = true, IsActive = false, LocationId = 2 }
                );
                await context.SaveChangesAsync();
            }
            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            var results = await service.GetAllAsync();

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);

            var userA = results.FirstOrDefault(u => u.Name == "User A");
            Assert.IsNotNull(userA);
            Assert.AreEqual("idA", userA.AzureAdObjectId);
            Assert.IsFalse(userA.IsAdmin);
            Assert.IsTrue(userA.IsActive);

            var userB = results.FirstOrDefault(u => u.Name == "User B");
            Assert.IsNotNull(userB);
            Assert.AreEqual("idB", userB.AzureAdObjectId);
            Assert.IsTrue(userB.IsAdmin);
            Assert.IsFalse(userB.IsActive);
        }

        [TestMethod]
        public async Task GetAllAsync_WithLocations_PopulatesLocationDetails()
        {
            // Arrange
            using (var context = CreateContext())
            {
                context.Locations.AddRange(
                    new Location { Id = 1, Name = "Location A" },
                    new Location { Id = 2, Name = "Location B" }
                );
                context.AppUsers.AddRange(
                    new AppUser { Name = "User With Loc", AzureAdObjectId = "id1", LocationId = 1 },
                    new AppUser { Name = "User Without Loc", AzureAdObjectId = "id2", LocationId = 0 }, // Assuming 0 or null means no location
                    new AppUser { Name = "User With Invalid Loc", AzureAdObjectId = "id3", LocationId = 99 } // Non-existent LocationId
                );
                await context.SaveChangesAsync();
            }
            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            var results = await service.GetAllAsync();

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);

            var userWithLoc = results.FirstOrDefault(u => u.Name == "User With Loc");
            Assert.IsNotNull(userWithLoc);
            Assert.AreEqual(1, userWithLoc.LocationId);
            Assert.AreEqual("Location A", userWithLoc.LocationName);

            var userWithoutLoc = results.FirstOrDefault(u => u.Name == "User Without Loc");
            Assert.IsNotNull(userWithoutLoc);
            Assert.AreEqual(0, userWithoutLoc.LocationId);
            Assert.AreEqual(string.Empty, userWithoutLoc.LocationName); // Expect empty string for no/invalid location

            var userWithInvalidLoc = results.FirstOrDefault(u => u.Name == "User With Invalid Loc");
            Assert.IsNotNull(userWithInvalidLoc);
            Assert.AreEqual(99, userWithInvalidLoc.LocationId);
            Assert.AreEqual(string.Empty, userWithInvalidLoc.LocationName); // Expect empty string
        }

        [TestMethod]
        public async Task GetByAzureAdObjectIdAsync_WithLocation_PopulatesLocationDetails()
        {
            // Arrange
            var azureIdWithLoc = "userWithLocation";
            var azureIdNoLoc = "userWithoutLocation";
            var azureIdInvalidLoc = "userWithInvalidLocation";

            using (var context = CreateContext())
            {
                context.Locations.Add(new Location { Id = 1, Name = "Test Location" });
                context.AppUsers.AddRange(
                    new AppUser { AzureAdObjectId = azureIdWithLoc, Name = "User With Loc", LocationId = 1 },
                    new AppUser { AzureAdObjectId = azureIdNoLoc, Name = "User No Loc", LocationId = 0 },
                    new AppUser { AzureAdObjectId = azureIdInvalidLoc, Name = "User Invalid Loc", LocationId = 99 }
                );
                await context.SaveChangesAsync();
            }
            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            var resultWithLoc = await service.GetByAzureAdObjectIdAsync(azureIdWithLoc);
            var resultNoLoc = await service.GetByAzureAdObjectIdAsync(azureIdNoLoc);
            var resultInvalidLoc = await service.GetByAzureAdObjectIdAsync(azureIdInvalidLoc);

            // Assert
            Assert.IsNotNull(resultWithLoc);
            Assert.AreEqual(1, resultWithLoc.LocationId);
            Assert.AreEqual("Test Location", resultWithLoc.LocationName);

            Assert.IsNotNull(resultNoLoc);
            Assert.AreEqual(0, resultNoLoc.LocationId);
            Assert.AreEqual(string.Empty, resultNoLoc.LocationName);

            Assert.IsNotNull(resultInvalidLoc);
            Assert.AreEqual(99, resultInvalidLoc.LocationId);
            Assert.AreEqual(string.Empty, resultInvalidLoc.LocationName);
        }

        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_NewUser_PopulatesDefaultLocation()
        {
            // Arrange
            var azureId = "newUserWithDefaultLoc";
            var userName = "New User Default Loc";
            var claims = new List<Claim>
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", azureId),
                new Claim(ClaimTypes.Name, userName)
            };
            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));

            using (var context = CreateContext())
            {
                context.Locations.Add(new Location { Id = 1, Name = "Default Location" });
                await context.SaveChangesAsync();
            }
            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            var resultDto = await service.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);

            // Assert
            Assert.IsNotNull(resultDto);
            Assert.AreEqual(userName, resultDto.Name);
            Assert.AreEqual(azureId, resultDto.AzureAdObjectId);
            Assert.AreEqual(1, resultDto.LocationId); // Default LocationId
            Assert.AreEqual("Default Location", resultDto.LocationName);

            using (var context = CreateContext())
            {
                var dbUser = await context.AppUsers.Include(u => u.Location).FirstOrDefaultAsync(u => u.AzureAdObjectId == azureId);
                Assert.IsNotNull(dbUser);
                Assert.AreEqual(1, dbUser.LocationId);
                Assert.IsNotNull(dbUser.Location);
                Assert.AreEqual("Default Location", dbUser.Location.Name);
            }
        }

        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_NewUser_NoDefaultLocationExists_LocationNameIsEmpty()
        {
            // Arrange
            var azureId = "newUserNoDefaultLoc";
            var userName = "New User No Default Loc";
            var claims = new List<Claim>
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", azureId),
                new Claim(ClaimTypes.Name, userName)
            };
            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
            // No locations added to the database for this test

            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            var resultDto = await service.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);

            // Assert
            Assert.IsNotNull(resultDto);
            Assert.AreEqual(1, resultDto.LocationId); // Still defaults to LocationId = 1
            Assert.AreEqual(string.Empty, resultDto.LocationName); // But name is empty as location doesn't exist

            using (var context = CreateContext())
            {
                var dbUser = await context.AppUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == azureId);
                Assert.IsNotNull(dbUser);
                Assert.AreEqual(1, dbUser.LocationId);
            }
        }

        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_ExistingUserWithLocation_PopulatesLocationDetails()
        {
            // Arrange
            var azureId = "existingUserWithLoc";
            var userName = "Existing User";
            var claims = new List<Claim>
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", azureId),
                new Claim(ClaimTypes.Name, userName)
            };
            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));

            using (var context = CreateContext())
            {
                context.Locations.Add(new Location { Id = 7, Name = "Assigned Location" });
                context.AppUsers.Add(new AppUser {
                    AzureAdObjectId = azureId,
                    Name = userName,
                    LocationId = 7 // Pre-existing location
                });
                await context.SaveChangesAsync();
            }
            var service = new AppUserService(_mockDbContextFactory.Object);

            // Act
            var resultDto = await service.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);

            // Assert
            Assert.IsNotNull(resultDto);
            Assert.AreEqual(7, resultDto.LocationId);
            Assert.AreEqual("Assigned Location", resultDto.LocationName);
        }
    }
}
