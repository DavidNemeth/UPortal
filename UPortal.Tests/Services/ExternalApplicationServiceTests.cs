// UPortal.Tests/Services/ExternalApplicationServiceTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using UPortal.Data;
using UPortal.Data.Models;
using UPortal.Dtos;
using UPortal.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting; // Using MSTest

namespace UPortal.Tests.Services
{
    [TestClass]
    public class ExternalApplicationServiceTests
    {
        private Mock<IDbContextFactory<ApplicationDbContext>> _mockDbContextFactory;
        private Mock<ApplicationDbContext> _mockDbContext;
        private Mock<DbSet<ExternalApplication>> _mockDbSet;
        private Mock<ILogger<ExternalApplicationService>> _mockLogger;
        private ExternalApplicationService _service;
        private DbContextOptions<ApplicationDbContext> _options;


        [TestInitialize]
        public void Initialize()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _mockDbContext = new Mock<ApplicationDbContext>(_options);
            _mockDbSet = new Mock<DbSet<ExternalApplication>>();

            _mockDbContext.Setup(db => db.ExternalApplications).Returns(_mockDbSet.Object);

            _mockDbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
            _mockDbContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(_mockDbContext.Object);

            _mockLogger = new Mock<ILogger<ExternalApplicationService>>();
            _service = new ExternalApplicationService(_mockDbContextFactory.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task AddAsync_DatabaseErrorOnSaveChanges_ThrowsDbUpdateExceptionAndLogsError()
        {
            // Arrange
            var dto = new ExternalApplicationDto { AppName = "Test App", AppUrl = "http://test.com", IconName = "test-icon" };
            var dbUpdateException = new DbUpdateException("Simulated DB error during AddAsync", new Exception("Inner"));

            _mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
                          .ThrowsAsync(dbUpdateException);

            // Act & Assert
            var caughtException = await Assert.ThrowsExceptionAsync<DbUpdateException>(() => _service.AddAsync(dto));

            Assert.AreSame(dbUpdateException, caughtException);

            _mockLogger.VerifyLogging(
                LogLevel.Error,
                $"Error adding application {dto.AppName} to the database.",
                Times.Once(),
                expectedException: dbUpdateException);
        }

        [TestMethod]
        public async Task DeleteAsync_ApplicationNotFound_LogsWarning()
        {
            // Arrange
            var appId = 1;
            _mockDbSet.Setup(s => s.FindAsync(It.IsAny<object[]>()))
                      .ReturnsAsync((ExternalApplication?)null); // Simulate app not found

            // Act
            await _service.DeleteAsync(appId);

            // Assert
            _mockLogger.VerifyLogging(
                LogLevel.Warning,
                $"Application with Id: {appId} not found for deletion.",
                Times.Once());
        }

        [TestMethod]
        public async Task DeleteAsync_DatabaseErrorOnSaveChanges_ThrowsDbUpdateExceptionAndLogsError()
        {
            // Arrange
            var appId = 1;
            var appToDelete = new ExternalApplication { Id = appId, AppName = "Test App" };
            var dbUpdateException = new DbUpdateException("Simulated DB error during DeleteAsync", new Exception("Inner"));

            _mockDbSet.Setup(s => s.FindAsync(appId)) // Ensure FindAsync is mocked with specific ID if needed, or It.IsAny
                      .ReturnsAsync(appToDelete);
            _mockDbContext.Setup(db => db.ExternalApplications.Remove(appToDelete)); // Mock the Remove call
            _mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
                          .ThrowsAsync(dbUpdateException);

            // Act & Assert
            var caughtException = await Assert.ThrowsExceptionAsync<DbUpdateException>(() => _service.DeleteAsync(appId));

            Assert.AreSame(dbUpdateException, caughtException);

            _mockLogger.VerifyLogging(
                LogLevel.Error,
                $"Error deleting application with Id: {appId} from the database.",
                Times.Once(),
                expectedException: dbUpdateException);
        }
    }
}
