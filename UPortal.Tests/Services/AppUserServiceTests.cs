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
// UPortal.Tests/Services/AppUserServiceTests.cs
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
using Microsoft.Extensions.Logging;

namespace UPortal.Tests.Services
{
    [TestClass]
    public class AppUserServiceTests
    {
        private DbContextOptions<ApplicationDbContext> _options;
        private Mock<IDbContextFactory<ApplicationDbContext>> _mockDbContextFactory;
        private Mock<ILogger<AppUserService>> _mockLogger;
        private AppUserService _userService;

        [TestInitialize]
        public void Initialize()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var dbContext = new ApplicationDbContext(_options);
            dbContext.Database.EnsureCreated();

            _mockDbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            _mockDbContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(() => new ApplicationDbContext(_options));

            _mockLogger = new Mock<ILogger<AppUserService>>();

            _userService = new AppUserService(_mockDbContextFactory.Object, _mockLogger.Object);
        }

        private ApplicationDbContext CreateContext() => new ApplicationDbContext(_options);

        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_NullClaimsPrincipal_ThrowsArgNullExceptionAndLogsError()
        {
            // Arrange
            ClaimsPrincipal? principal = null;

            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _userService.CreateOrUpdateUserFromAzureAdAsync(principal!));

            Assert.AreEqual("userPrincipal", exception.ParamName);

            _mockLogger.VerifyLogging(
                LogLevel.Error,
                "userPrincipal cannot be null",
                Times.Once());
        }

        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_MissingAzureAdObjectIdClaim_ThrowsArgumentNullExceptionAndLogsError()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Test User") }; // No ObjectIdentifier claim
            var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _userService.CreateOrUpdateUserFromAzureAdAsync(userPrincipal));

            Assert.AreEqual("azureAdObjectId", exception.ParamName); // Service throws on azureAdObjectId being null/empty

            _mockLogger.VerifyLogging(
                LogLevel.Error,
                "Azure AD Object ID not found in claims",
                Times.Once());
        }

        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_DatabaseErrorOnSave_ThrowsDbUpdateExceptionAndLogsError()
        {
            // Arrange
            var azureId = "dbErrorUser";
            var userName = "DB Error User";
            var claims = new List<Claim>
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", azureId),
                new Claim(ClaimTypes.Name, userName)
            };
            var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(claimsIdentity);

            var mockDbContext = new Mock<ApplicationDbContext>(_options); // _options is from TestInitialize

            // Setup AppUsers DbSet mock
            var mockAppUsersDbSet = new Mock<DbSet<AppUser>>();
            mockDbContext.Setup(db => db.AppUsers).Returns(mockAppUsersDbSet.Object);

            // Specifically for FirstOrDefaultAsync to return null (new user)
            // This requires more complex setup if using Moq.EntityFrameworkCore, or manual mocking for async.
            // For simplicity, we'll assume the path to SaveChangesAsync is reached for a new user.
            // If FirstOrDefaultAsync itself could throw, that's a different test.
            // We'll mock SaveChangesAsync to throw.
            mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
                         .ThrowsAsync(new DbUpdateException("Simulated DB update error", new Exception("Inner sim error")));

            _mockDbContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(mockDbContext.Object);

            var serviceWithMockedContext = new AppUserService(_mockDbContextFactory.Object, _mockLogger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<DbUpdateException>(
                () => serviceWithMockedContext.CreateOrUpdateUserFromAzureAdAsync(userPrincipal));

            Assert.AreEqual("Simulated DB update error", exception.Message);

            _mockLogger.VerifyLogging(
                LogLevel.Error,
                $"Database error occurred in CreateOrUpdateUserFromAzureAdAsync for AzureAdObjectId: {azureId}",
                Times.Once(),
                expectedException: exception); // Verify the specific exception instance was logged
        }

        [TestMethod]
        public async Task UpdateAppUserAsync_UserNotFound_ThrowsKeyNotFoundExceptionAndLogsError()
        {
            // Arrange
            var nonExistentUserId = 999;
            var userToUpdateDto = new UpdateAppUserDto { IsActive = true, IsAdmin = false, LocationId = 1 };

            var mockDbContext = new Mock<ApplicationDbContext>(_options);
            // Setup FindAsync to return null, simulating user not found
            mockDbContext.Setup(db => db.AppUsers.FindAsync(nonExistentUserId))
                         .ReturnsAsync((AppUser?)null);
            // If your FindAsync is not directly on DbSet but an extension or part of a repository pattern, adjust mocking accordingly.
            // For DbContext.FindAsync, it's harder to mock directly without a concrete instance or further abstraction.
            // The above setup for FindAsync might not work directly with EF Core's FindAsync.
            // A more robust way for in-memory or if you control the context directly:
            // Ensure the in-memory DB is empty or doesn't contain the user.
            // For this test, we'll rely on the service calling FindAsync and it returning null.
            // If AppUsers.FindAsync is called, it needs to be on a DbSet that can be mocked.
            // Let's refine the DbContext mock for FindAsync
            var mockAppUsersDbSet = new Mock<DbSet<AppUser>>();
            mockAppUsersDbSet.Setup(m => m.FindAsync(It.IsAny<object[]>())) // It.IsAny<object[]>() for the key values
                .Returns(new ValueTask<AppUser?>((AppUser?)null)); // Return null for any key
            mockDbContext.Setup(db => db.AppUsers).Returns(mockAppUsersDbSet.Object);


            _mockDbContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(mockDbContext.Object);

            var serviceWithMockedContext = new AppUserService(_mockDbContextFactory.Object, _mockLogger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => serviceWithMockedContext.UpdateAppUserAsync(nonExistentUserId, userToUpdateDto));

            Assert.IsTrue(exception.Message.Contains($"User with ID {nonExistentUserId} not found."));

            _mockLogger.VerifyLogging(
                LogLevel.Error,
                $"User with ID {nonExistentUserId} not found for update.",
                Times.Once());
        }


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

            // Act
            var result = await _userService.GetByAzureAdObjectIdAsync(azureId);

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
            // Arrange Act
            var result = await _userService.GetByAzureAdObjectIdAsync("nonExistentId");

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

            // Act
            var resultDto = await _userService.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);

            // Assert
            Assert.IsNotNull(resultDto);
            Assert.AreEqual(userName, resultDto.Name);
            Assert.AreEqual(azureId, resultDto.AzureAdObjectId);
            Assert.IsFalse(resultDto.IsAdmin);
            Assert.IsTrue(resultDto.IsActive);

            using (var context = CreateContext())
            {
                var dbUser = await context.AppUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == azureId);
                Assert.IsNotNull(dbUser);
                Assert.AreEqual(userName, dbUser.Name);
                Assert.AreEqual(1, dbUser.LocationId);
            }
        }

        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_ExistingUser_ReturnsExistingUserDtoAndDoesNotAlterSensitiveFields()
        {
            // Arrange
            var azureId = "existingAzureUser";
            var originalName = "Original Name";
            var updatedNameFromClaims = "Updated Name From Claims";

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
                new Claim(ClaimTypes.Name, updatedNameFromClaims)
            };
            var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Act
            var resultDto = await _userService.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);

            // Assert
            Assert.IsNotNull(resultDto);
            Assert.AreEqual(originalName, resultDto.Name);
            Assert.AreEqual(azureId, resultDto.AzureAdObjectId);
            Assert.IsTrue(resultDto.IsAdmin);
            Assert.IsFalse(resultDto.IsActive);

            using (var context = CreateContext())
            {
                var dbUser = await context.AppUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == azureId);
                Assert.IsNotNull(dbUser);
                Assert.AreEqual(originalName, dbUser.Name);
                Assert.IsTrue(dbUser.IsAdmin);
                Assert.IsFalse(dbUser.IsActive);
                Assert.AreEqual(5, dbUser.LocationId);
                Assert.AreEqual(1, context.AppUsers.Count());
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CreateOrUpdateUserFromAzureAdAsync_NullAzureAdObjectId_ThrowsArgumentNullException()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Test User") };
            var claimsIdentity = new ClaimsIdentity(claims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Act
            await _userService.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);
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

            // Act
            await _userService.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CreateOrUpdateUserFromAzureAdAsync_NullPrincipal_ThrowsArgumentNullException()
        {
            // Arrange Act
            await _userService.CreateOrUpdateUserFromAzureAdAsync(null!);
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
            var dto = new UpdateAppUserDto
            {
                IsActive = false,
                IsAdmin = true,
                LocationId = updatedLocationId
            };

            // Act
            await _userService.UpdateAppUserAsync(userId, dto);

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
            var dto = new UpdateAppUserDto { IsActive = true, IsAdmin = false, LocationId = 1 };

            // Act
            await _userService.UpdateAppUserAsync(999, dto);
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

            // Act
            var results = await _userService.GetAllAsync();

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
                    new AppUser { Name = "User Without Loc", AzureAdObjectId = "id2", LocationId = 0 },
                    new AppUser { Name = "User With Invalid Loc", AzureAdObjectId = "id3", LocationId = 99 }
                );
                await context.SaveChangesAsync();
            }

            // Act
            var results = await _userService.GetAllAsync();

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
            Assert.AreEqual(string.Empty, userWithoutLoc.LocationName);

            var userWithInvalidLoc = results.FirstOrDefault(u => u.Name == "User With Invalid Loc");
            Assert.IsNotNull(userWithInvalidLoc);
            Assert.AreEqual(99, userWithInvalidLoc.LocationId);
            Assert.AreEqual(string.Empty, userWithInvalidLoc.LocationName);
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

            // Act
            var resultWithLoc = await _userService.GetByAzureAdObjectIdAsync(azureIdWithLoc);
            var resultNoLoc = await _userService.GetByAzureAdObjectIdAsync(azureIdNoLoc);
            var resultInvalidLoc = await _userService.GetByAzureAdObjectIdAsync(azureIdInvalidLoc);

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

            // Act
            var resultDto = await _userService.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);

            // Assert
            Assert.IsNotNull(resultDto);
            Assert.AreEqual(userName, resultDto.Name);
            Assert.AreEqual(azureId, resultDto.AzureAdObjectId);
            Assert.AreEqual(1, resultDto.LocationId);
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

            // Act
            var resultDto = await _userService.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);

            // Assert
            Assert.IsNotNull(resultDto);
            Assert.AreEqual(1, resultDto.LocationId);
            Assert.AreEqual(string.Empty, resultDto.LocationName);

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
                    LocationId = 7
                });
                await context.SaveChangesAsync();
            }

            // Act
            var resultDto = await _userService.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);

            // Assert
            Assert.IsNotNull(resultDto);
            Assert.AreEqual(7, resultDto.LocationId);
            Assert.AreEqual("Assigned Location", resultDto.LocationName);
        }
    }
}
