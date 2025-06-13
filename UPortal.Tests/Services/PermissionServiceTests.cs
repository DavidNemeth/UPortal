using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using UPortal.Data;
using UPortal.Data.Models;
using UPortal.Dtos;
using UPortal.Services;
using Xunit;
using System.Linq; // Required for Count() and Assert.Contains
using System.Threading.Tasks; // Required for Task
using System.Collections.Generic; // Required for IEnumerable

namespace UPortal.Tests.Services
{
    public class PermissionServiceTests : IDisposable
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private ApplicationDbContext _context; // Non-readonly to allow re-creation
        private PermissionService _permissionService;
        private readonly Mock<ILogger<PermissionService>> _mockLogger;
        private readonly IDbContextFactory<ApplicationDbContext> _mockContextFactory;


        public PermissionServiceTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString()) // Unique DB name
                .Options;

            _mockLogger = new Mock<ILogger<PermissionService>>();

            // Mock IDbContextFactory
            var mockFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                       .Returns(() => Task.FromResult(new ApplicationDbContext(_options))); // Return new context instance for async
            mockFactory.Setup(f => f.CreateDbContext())
                       .Returns(() => new ApplicationDbContext(_options)); // For synchronous (if ever used by service)
            _mockContextFactory = mockFactory.Object;

            // Initialize context and service here for shared setup if some tests don't need fresh context
            // For tests needing pristine context, it will be re-initialized in the test method.
            InitializeNewContextAndService();
        }

        private void InitializeNewContextAndService()
        {
            // Dispose existing context if it's not null
            _context?.Dispose();
            _context = new ApplicationDbContext(_options);
            _context.Database.EnsureCreated(); // Ensure schema is created for in-memory
             _permissionService = new PermissionService(_mockContextFactory, _mockLogger.Object);
        }


        private async Task SeedPermissionsAsync(params string[] permissionNames)
        {
            // Use the current _context instance for seeding
            foreach (var name in permissionNames)
            {
                _context.Permissions.Add(new Permission { Name = name });
            }
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAllPermissionsAsync_ShouldReturnAllPermissions_WhenPermissionsExist()
        {
            // Arrange
            InitializeNewContextAndService(); // Ensure fresh context
            await SeedPermissionsAsync("Perm1", "Perm2");

            // Act
            var result = await _permissionService.GetAllPermissionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, p => p.Name == "Perm1");
            Assert.Contains(result, p => p.Name == "Perm2");
        }

        [Fact]
        public async Task GetAllPermissionsAsync_ShouldReturnEmptyList_WhenNoPermissionsExist()
        {
            // Arrange
            InitializeNewContextAndService(); // Ensure fresh context, no seeding

            // Act
            var result = await _permissionService.GetAllPermissionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPermissionByIdAsync_ShouldReturnPermission_WhenPermissionExists()
        {
            // Arrange
            InitializeNewContextAndService(); // Ensure fresh context
            await SeedPermissionsAsync("TestPerm");
            var permission = await _context.Permissions.FirstAsync(p => p.Name == "TestPerm");

            // Act
            var result = await _permissionService.GetPermissionByIdAsync(permission.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(permission.Id, result.Id);
            Assert.Equal("TestPerm", result.Name);
        }

        [Fact]
        public async Task GetPermissionByIdAsync_ShouldReturnNull_WhenPermissionDoesNotExist()
        {
            // Arrange
            InitializeNewContextAndService(); // Ensure fresh context

            // Act
            var result = await _permissionService.GetPermissionByIdAsync(-1); // Non-existent ID

            // Assert
            Assert.Null(result);
        }

        public void Dispose()
        {
            _context?.Database.EnsureDeleted(); // Clean up the in-memory database
            _context?.Dispose();
        }
    }
}
