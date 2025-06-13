using System.Net;
using System.Net.Http; // For HttpClient
using System.Net.Http.Json; // For ReadFromJsonAsync
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UPortal.Dtos;
using System.Collections.Generic;
using UPortal.Tests.Helpers; // Added
using Microsoft.AspNetCore.TestHost; // Added
using Microsoft.Extensions.DependencyInjection; // Added
using Microsoft.AspNetCore.Authentication; // Added
using Moq; // Added
using UPortal.Services; // Required for IAppUserService, IMachineService, ILocationService
using System.Linq; // Required for Enumerable.Empty and .All()


namespace UPortal.Tests.Controllers
{
    [TestClass]
    public class UserInfoControllerTests
    {
        private WebApplicationFactory<UPortal.Program> _factory;
        private HttpClient _client;
        private Mock<IAppUserService> _mockAppUserService;
        private Mock<IMachineService> _mockMachineService;
        private Mock<ILocationService> _mockLocationService;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockAppUserService = new Mock<IAppUserService>();
            _mockMachineService = new Mock<IMachineService>();
            _mockLocationService = new Mock<ILocationService>();

            _factory = new WebApplicationFactory<UPortal.Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                            .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(
                                TestAuthHandler.AuthenticationScheme, options => {
                                    options.DefaultAzureAdObjectId = "test-azure-ad-object-id"; // Consistent ID
                                });

                        // Replace real services with mocks
                        services.AddScoped<IAppUserService>(_ => _mockAppUserService.Object);
                        services.AddScoped<IMachineService>(_ => _mockMachineService.Object);
                        services.AddScoped<ILocationService>(_ => _mockLocationService.Object);
                    });
                });
            _client = _factory.CreateClient();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        [TestMethod]
        public async Task GetCurrentUser_Unauthenticated_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/userinfo/me");

            // Assert
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Response status code should be Unauthorized.");
        }

        [TestMethod]
        public async Task Ping_Unauthenticated_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/userinfo/ping");

            // Assert
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Response status code should be Unauthorized.");
        }

        [TestMethod]
        public async Task GetMachines_Unauthenticated_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/userinfo/machines");

            // Assert
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Response status code should be Unauthorized.");
        }

        [TestMethod]
        public async Task GetLocations_Unauthenticated_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/userinfo/locations");

            // Assert
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Response status code should be Unauthorized.");
        }

        // TODO: Add tests for authenticated scenarios. This will require setting up
        // a way to send authenticated requests. Common approaches include:
        // 1. Mocking the authentication handler (e.g., using a TestAuthHandler).
        //    This is often the most robust way for integration tests.
        //    Example: services.AddAuthentication("TestScheme")
        //                     .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => {});
        //    And then adding a header to the client: _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
        //
        // 2. Configuring the WebApplicationFactory to use a test user/principal for all requests.
        //    builder.ConfigureTestServices(services => services.AddScoped<IUserProfileService, TestUserProfileService>());

        // Example structure for an authenticated test (requires TestAuthHandler or similar):
        // [TestMethod]
        // public async Task GetCurrentUser_Authenticated_ReturnsOkAndUserData()
        // {
        //     // Arrange: Ensure _client is set up for authenticated requests
        //     // (e.g., by configuring TestAuthHandler in WithWebHostBuilder)
        //     // _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("TestScheme");

        //     // Act
        //     var response = await _client.GetAsync("/api/userinfo/me");

        //     // Assert
        //     Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        //     var user = await response.Content.ReadFromJsonAsync<AppUserDto>();
        //     Assert.IsNotNull(user);
        //     Assert.AreEqual("Test User", user.Name); // Assuming TestAuthHandler provides these claims
        // }

        [TestMethod]
        public async Task Ping_Authenticated_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/userinfo/ping");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.AreEqual("Pong from UserInfoController", content);
        }

        [TestMethod]
        public async Task GetCurrentUser_Authenticated_ReturnsOkAndUserData()
        {
            // Arrange
            var expectedUser = new AppUserDto {
                Id = 1,
                Name = "Test User",
                AzureAdObjectId = "test-azure-ad-object-id",
                LocationName = "Test Location",
                IsActive = true,
                IsAdmin = false
            };
            _mockAppUserService.Setup(s => s.GetByAzureAdObjectIdAsync("test-azure-ad-object-id"))
                .ReturnsAsync(expectedUser);

            // Act
            var response = await _client.GetAsync("/api/userinfo/me");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var actualUser = await response.Content.ReadFromJsonAsync<AppUserDto>();
            Assert.IsNotNull(actualUser);
            Assert.AreEqual(expectedUser.Name, actualUser.Name);
            Assert.AreEqual(expectedUser.AzureAdObjectId, actualUser.AzureAdObjectId);
            Assert.AreEqual(expectedUser.LocationName, actualUser.LocationName);
            Assert.AreEqual(expectedUser.IsActive, actualUser.IsActive);
            Assert.AreEqual(expectedUser.IsAdmin, actualUser.IsAdmin);
        }

        [TestMethod]
        public async Task GetMachines_Authenticated_ReturnsMachineData()
        {
            // Arrange
            var expectedMachines = new List<MachineDto>
            {
                new MachineDto { Id = 1, Name = "Test Machine 1", AppUserId = 1, LocationName = "Location A" },
                new MachineDto { Id = 2, Name = "Test Machine 2", AppUserId = 1, LocationName = "Location B" }
            };
            _mockMachineService.Setup(s => s.GetAllAsync()).ReturnsAsync(expectedMachines);

            // Act
            var response = await _client.GetAsync("/api/userinfo/machines");

            // Assert
            response.EnsureSuccessStatusCode();
            var actualMachines = await response.Content.ReadFromJsonAsync<List<MachineDto>>();
            Assert.IsNotNull(actualMachines);
            Assert.AreEqual(expectedMachines.Count, actualMachines.Count);
            // Add more detailed assertions if necessary, e.g., comparing properties of individual machines
            Assert.AreEqual(expectedMachines[0].Name, actualMachines[0].Name);
        }

        [TestMethod]
        public async Task GetMachines_Authenticated_WithUserIdFilter_ReturnsFilteredData()
        {
            // Arrange
            var userIdToFilter = 10;
            var allMachines = new List<MachineDto>
            {
                new MachineDto { Id = 1, Name = "Machine1", AppUserId = userIdToFilter, LocationName = "LocationA" },
                new MachineDto { Id = 2, Name = "Machine2", AppUserId = 20, LocationName = "LocationB" },
                new MachineDto { Id = 3, Name = "Machine3", AppUserId = userIdToFilter, LocationName = "LocationC" }
            };
            // Note: The controller's GetMachines method calls GetAllAsync and then filters in memory.
            // So, the mock setup is for GetAllAsync.
            _mockMachineService.Setup(s => s.GetAllAsync()).ReturnsAsync(allMachines);

            // Act
            var response = await _client.GetAsync($"/api/userinfo/machines?userId={userIdToFilter}");

            // Assert
            response.EnsureSuccessStatusCode();
            var filteredMachines = await response.Content.ReadFromJsonAsync<List<MachineDto>>();
            Assert.IsNotNull(filteredMachines);
            Assert.AreEqual(2, filteredMachines.Count, "Should only return machines for the specified user.");
            Assert.IsTrue(filteredMachines.All(m => m.AppUserId == userIdToFilter), "All returned machines should have the correct AppUserId.");
        }

        [TestMethod]
        public async Task GetMachines_Authenticated_WithUserIdFilter_NoMatchingMachines_ReturnsEmptyList()
        {
            // Arrange
            var userIdToFilter = 999; // A user ID that has no machines
            var allMachines = new List<MachineDto>
            {
                new MachineDto { Id = 1, Name = "Machine1", AppUserId = 10, LocationName = "LocationA" },
                new MachineDto { Id = 2, Name = "Machine2", AppUserId = 20, LocationName = "LocationB" }
            };
            _mockMachineService.Setup(s => s.GetAllAsync()).ReturnsAsync(allMachines);

            // Act
            var response = await _client.GetAsync($"/api/userinfo/machines?userId={userIdToFilter}");

            // Assert
            response.EnsureSuccessStatusCode();
            var filteredMachines = await response.Content.ReadFromJsonAsync<List<MachineDto>>();
            Assert.IsNotNull(filteredMachines);
            Assert.AreEqual(0, filteredMachines.Count, "Should return an empty list if no machines match the filter.");
        }


        [TestMethod]
        public async Task GetLocations_Authenticated_ReturnsLocationData()
        {
            // Arrange
            var expectedLocations = new List<LocationDto>
            {
                new LocationDto { Id = 1, Name = "Test Location A", MachineCount = 2, UserCount = 5 },
                new LocationDto { Id = 2, Name = "Test Location B", MachineCount = 1, UserCount = 3 }
            };
            _mockLocationService.Setup(s => s.GetAllAsync()).ReturnsAsync(expectedLocations);

            // Act
            var response = await _client.GetAsync("/api/userinfo/locations");

            // Assert
            response.EnsureSuccessStatusCode();
            var actualLocations = await response.Content.ReadFromJsonAsync<List<LocationDto>>();
            Assert.IsNotNull(actualLocations);
            Assert.AreEqual(expectedLocations.Count, actualLocations.Count);
            // Add more detailed assertions if necessary
            Assert.AreEqual(expectedLocations[0].Name, actualLocations[0].Name);
        }
    }
}
