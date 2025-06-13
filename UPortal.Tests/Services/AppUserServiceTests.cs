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
using System.Threading; // Required for CancellationToken

namespace UPortal.Tests.Services
{
    // Helper extension for Moq ILogger verification
    public static class LoggerVerifyExtensions
    {
        public static void VerifyLogging<T>(this Mock<ILogger<T>> mockLogger, LogLevel expectedLogLevel, string expectedMessageContains, Times? times = null, Exception? expectedException = null)
        {
            times ??= Times.AtLeastOnce();

            mockLogger.Verify(
                x => x.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessageContains)),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                times.Value);
        }
    }

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

            // Seed a default location for tests that might require it due to FK constraints
            using (var context = new ApplicationDbContext(_options))
            {
                context.Database.EnsureCreated(); // Important for In-Memory
                if (!context.Locations.Any(l => l.Id == 1))
                {
                    context.Locations.Add(new Location { Id = 1, Name = "Default Location" });
                    context.SaveChanges();
                }
            }

            _mockDbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            // Configure the factory to return a new context instance each time, using the same options.
            _mockDbContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new ApplicationDbContext(_options));
            _mockDbContextFactory.Setup(f => f.CreateDbContext())
                .Returns(() => new ApplicationDbContext(_options));


            _mockLogger = new Mock<ILogger<AppUserService>>();
            _userService = new AppUserService(_mockDbContextFactory.Object, _mockLogger.Object);
        }

        private ApplicationDbContext CreateContext() => new ApplicationDbContext(_options);

        // Existing tests from the original file (verified they are here)
        #region Existing AppUserService Tests
        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_NullClaimsPrincipal_ThrowsArgNullExceptionAndLogsError()
        {
            ClaimsPrincipal? principal = null;
            var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _userService.CreateOrUpdateUserFromAzureAdAsync(principal!));
            Assert.AreEqual("userPrincipal", exception.ParamName);
            _mockLogger.VerifyLogging(LogLevel.Error, "userPrincipal cannot be null", Times.Once());
        }

        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_MissingAzureAdObjectIdClaim_ThrowsArgumentNullExceptionAndLogsError()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Test User") };
            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
            var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _userService.CreateOrUpdateUserFromAzureAdAsync(userPrincipal));
            Assert.AreEqual("azureAdObjectId", exception.ParamName);
            _mockLogger.VerifyLogging(LogLevel.Error, "Azure AD Object ID not found in claims", Times.Once());
        }

        [TestMethod]
        public async Task UpdateAppUserAsync_UserNotFound_ThrowsKeyNotFoundExceptionAndLogsError()
        {
            var nonExistentUserId = 999;
            var userToUpdateDto = new UpdateAppUserDto { IsActive = true, LocationId = 1 };
             // For this test, we ensure the user does not exist by using a fresh context from the factory
            var exception = await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => _userService.UpdateAppUserAsync(nonExistentUserId, userToUpdateDto));
            Assert.IsTrue(exception.Message.Contains($"User with ID {nonExistentUserId} not found."));
            _mockLogger.VerifyLogging(LogLevel.Error, $"User with ID {nonExistentUserId} not found for update.", Times.Once());
        }


        [TestMethod]
        public async Task GetByAzureAdObjectIdAsync_UserExists_ReturnsUserDto()
        {
            var azureId = "testAzureId1";
            using (var context = CreateContext())
            {
                context.AppUsers.Add(new AppUser { Name = "Test User 1", AzureAdObjectId = azureId, IsActive = true, LocationId = 1 });
                await context.SaveChangesAsync();
            }
            var result = await _userService.GetByAzureAdObjectIdAsync(azureId);
            Assert.IsNotNull(result);
            Assert.AreEqual("Test User 1", result.Name);
            Assert.AreEqual(azureId, result.AzureAdObjectId);
            Assert.IsTrue(result.IsActive);
        }

        [TestMethod]
        public async Task CreateOrUpdateUserFromAzureAdAsync_NewUser_CreatesAndReturnsUserDto()
        {
            var azureId = "newAzureUser";
            var userName = "New User Name";
            var claims = new List<Claim>
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", azureId),
                new Claim(ClaimTypes.Name, userName)
            };
            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
            var resultDto = await _userService.CreateOrUpdateUserFromAzureAdAsync(userPrincipal);
            Assert.IsNotNull(resultDto);
            Assert.AreEqual(userName, resultDto.Name);
            Assert.AreEqual(azureId, resultDto.AzureAdObjectId);
            Assert.IsTrue(resultDto.IsActive); // Default IsActive
            Assert.AreEqual(1, resultDto.LocationId); // Default LocationId
            using (var context = CreateContext())
            {
                var dbUser = await context.AppUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == azureId);
                Assert.IsNotNull(dbUser);
                Assert.AreEqual(userName, dbUser.Name);
                Assert.AreEqual(1, dbUser.LocationId);
            }
        }
        #endregion

        // Helper methods for seeding RBAC entities
        private async Task<AppUser> SeedUserAsync(ApplicationDbContext context, string name, string azureId = null)
        {
            azureId ??= Guid.NewGuid().ToString();
            var user = new AppUser { Name = name, AzureAdObjectId = azureId, LocationId = 1, IsActive = true };
            context.AppUsers.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        private async Task<Role> SeedRoleAsync(ApplicationDbContext context, string name)
        {
            var role = new Role { Name = name };
            context.Roles.Add(role);
            await context.SaveChangesAsync();
            return role;
        }

        private async Task<Permission> SeedPermissionAsync(ApplicationDbContext context, string name)
        {
            var permission = new Permission { Name = name };
            context.Permissions.Add(permission);
            await context.SaveChangesAsync();
            return permission;
        }

        // RBAC Tests
        [TestMethod]
        public async Task AssignRoleToUserAsync_ShouldCreateLink_WhenNotExists()
        {
            using var context = CreateContext();
            var user = await SeedUserAsync(context, "User1");
            var role = await SeedRoleAsync(context, "Role1");

            await _userService.AssignRoleToUserAsync(user.Id, role.Id);

            var linkExists = await context.UserRoles.AnyAsync(ur => ur.AppUserId == user.Id && ur.RoleId == role.Id);
            Assert.IsTrue(linkExists, "UserRole link should be created.");
        }

        [TestMethod]
        public async Task RemoveRoleFromUserAsync_ShouldRemoveLink_WhenExists()
        {
            using var context = CreateContext();
            var user = await SeedUserAsync(context, "User1");
            var role = await SeedRoleAsync(context, "Role1");
            context.UserRoles.Add(new UserRole { AppUserId = user.Id, RoleId = role.Id });
            await context.SaveChangesAsync();

            await _userService.RemoveRoleFromUserAsync(user.Id, role.Id);

            var linkExists = await context.UserRoles.AnyAsync(ur => ur.AppUserId == user.Id && ur.RoleId == role.Id);
            Assert.IsFalse(linkExists, "UserRole link should be removed.");
        }

        [TestMethod]
        public async Task GetRolesForUserAsync_ShouldReturnUserRoles_WithPermissions()
        {
            using var context = CreateContext();
            var user = await SeedUserAsync(context, "User1");
            var role1 = await SeedRoleAsync(context, "Role1");
            var perm1 = await SeedPermissionAsync(context, "Perm1");
            context.RolePermissions.Add(new RolePermission { RoleId = role1.Id, PermissionId = perm1.Id });
            context.UserRoles.Add(new UserRole { AppUserId = user.Id, RoleId = role1.Id });
            await context.SaveChangesAsync();

            var result = await _userService.GetRolesForUserAsync(user.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            var userRole = result.First();
            Assert.AreEqual("Role1", userRole.Name);
            Assert.AreEqual(1, userRole.Permissions.Count);
            Assert.AreEqual("Perm1", userRole.Permissions.First().Name);
        }

        [TestMethod]
        public async Task UserHasRoleAsync_ShouldReturnTrue_WhenUserHasRole()
        {
            using var context = CreateContext();
            var user = await SeedUserAsync(context, "User1");
            var role = await SeedRoleAsync(context, "TargetRole");
            context.UserRoles.Add(new UserRole { AppUserId = user.Id, RoleId = role.Id });
            await context.SaveChangesAsync();

            var result = await _userService.UserHasRoleAsync(user.Id, "TargetRole");
            Assert.IsTrue(result, "User should have the role 'TargetRole'.");
        }

        [TestMethod]
        public async Task UserHasRoleAsync_ShouldReturnFalse_WhenUserDoesNotHaveRole()
        {
            using var context = CreateContext();
            var user = await SeedUserAsync(context, "User1");
            await SeedRoleAsync(context, "OtherRole"); // Seed some other role

            var result = await _userService.UserHasRoleAsync(user.Id, "TargetRole"); // Check for a role user doesn't have
            Assert.IsFalse(result, "User should not have the role 'TargetRole'.");
        }

        [TestMethod]
        public async Task UserHasPermissionAsync_ShouldReturnTrue_WhenUserHasPermissionViaRole()
        {
            using var context = CreateContext();
            var user = await SeedUserAsync(context, "User1");
            var role = await SeedRoleAsync(context, "Editor");
            var permission = await SeedPermissionAsync(context, "CanEditContent");
            context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            context.UserRoles.Add(new UserRole { AppUserId = user.Id, RoleId = role.Id });
            await context.SaveChangesAsync();

            var result = await _userService.UserHasPermissionAsync(user.Id, "CanEditContent");
            Assert.IsTrue(result, "User should have permission 'CanEditContent'.");
        }

        [TestMethod]
        public async Task UserHasPermissionAsync_ShouldReturnFalse_WhenUserDoesNotHavePermission()
        {
            using var context = CreateContext();
            var user = await SeedUserAsync(context, "User1");
            var role = await SeedRoleAsync(context, "Viewer");
            var permission = await SeedPermissionAsync(context, "CanViewContent"); // User has this
            context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            context.UserRoles.Add(new UserRole { AppUserId = user.Id, RoleId = role.Id });
            await context.SaveChangesAsync();

            await SeedPermissionAsync(context, "CanEditContent"); // This permission exists but is not assigned to user's role

            var result = await _userService.UserHasPermissionAsync(user.Id, "CanEditContent");
            Assert.IsFalse(result, "User should not have permission 'CanEditContent'.");
        }

        [TestMethod]
        public async Task GetAllAsync_ShouldPopulateUserRolesAndPermissions()
        {
            using var context = CreateContext();
            var user1 = await SeedUserAsync(context, "User One", "azure1");
            var roleAdmin = await SeedRoleAsync(context, "Admin");
            var permManage = await SeedPermissionAsync(context, "ManageAll");
            context.RolePermissions.Add(new RolePermission { RoleId = roleAdmin.Id, PermissionId = permManage.Id });
            context.UserRoles.Add(new UserRole { AppUserId = user1.Id, RoleId = roleAdmin.Id });
            await context.SaveChangesAsync();

            var user2 = await SeedUserAsync(context, "User Two", "azure2"); // User with no roles

            var result = await _userService.GetAllAsync();

            Assert.AreEqual(2, result.Count());

            var user1Dto = result.FirstOrDefault(u => u.AzureAdObjectId == "azure1");
            Assert.IsNotNull(user1Dto);
            Assert.AreEqual(1, user1Dto.Roles.Count());
            var adminRoleDto = user1Dto.Roles.First();
            Assert.AreEqual("Admin", adminRoleDto.Name);
            Assert.AreEqual(1, adminRoleDto.Permissions.Count());
            Assert.AreEqual("ManageAll", adminRoleDto.Permissions.First().Name);

            var user2Dto = result.FirstOrDefault(u => u.AzureAdObjectId == "azure2");
            Assert.IsNotNull(user2Dto);
            Assert.AreEqual(0, user2Dto.Roles.Count());
        }
    }
}
