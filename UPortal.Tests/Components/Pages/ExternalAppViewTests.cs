using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;
using UPortal.Components.Pages;
using UPortal.Dtos;
using UPortal.Services;
using Xunit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components; // For NavigationManager
using System; // For TimeSpan
using System.Linq; // For Any()

namespace UPortal.Tests.Components.Pages
{
    public class ExternalAppViewTests : TestContext
    {
        private readonly Mock<IExternalApplicationService> _mockAppService;
        private readonly Mock<NavigationManager> _mockNavigationManager;

        public ExternalAppViewTests()
        {
            Services.AddFluentUIComponents(); // Essential for Fluent UI components

            _mockAppService = new Mock<IExternalApplicationService>();

            _mockNavigationManager = new Mock<NavigationManager>();
            _mockNavigationManager.Setup(m => m.NavigateTo(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()));
            _mockNavigationManager.Setup(m => m.BaseUri).Returns("http://localhost/");
            _mockNavigationManager.Setup(m => m.Uri).Returns("http://localhost/app/1"); // Example URI

            Services.AddSingleton(_mockAppService.Object);
            Services.AddSingleton(_mockNavigationManager.Object); // Register as the NavigationManager
        }

        private ExternalApplicationDto GetSampleApp(int id = 1, string name = "Test App", string url = "https://test.com") =>
            new ExternalApplicationDto { Id = id, AppName = name, AppUrl = url, IconName = "@Icons.Regular.Size24.Link" };

        [Fact]
        public async Task ExternalAppView_RendersLoadingState_ThenAppDetailsAndIframe()
        {
            // Arrange
            var sampleApp = GetSampleApp();
            var appId = sampleApp.Id;
            _mockAppService.Setup(s => s.GetByIdAsync(appId)).ReturnsAsync(sampleApp);

            // Act
            var cut = RenderComponent<ExternalAppView>(parameters => parameters
                .Add(p => p.AppId, appId)
            );

            // Assert: Initial loading state
            Assert.NotNull(cut.Find("fluent-progress-ring"));
            Assert.Contains("<em>Loading application details...</em>", cut.Markup);

            // Wait for the component to update after data loading
            await cut.WaitForStateAsync(() => cut.FindAll("iframe").Count == 1, TimeSpan.FromSeconds(2));

            // Assert: Application details and iframe are rendered
            Assert.Contains(sampleApp.AppName, cut.Find("h1").TextContent);
            var iframe = cut.Find("iframe");
            Assert.Equal(sampleApp.AppUrl, iframe.GetAttribute("src"));
            Assert.Equal("width: 100%; height: 100%; border: none;", iframe.GetAttribute("style"));
            Assert.True(iframe.HasAttribute("sandbox"));
        }

        [Fact]
        public async Task ExternalAppView_ShowsError_WhenAppNotFound()
        {
            // Arrange
            var appId = 99;
            _mockAppService.Setup(s => s.GetByIdAsync(appId)).ReturnsAsync((ExternalApplicationDto?)null);

            // Act
            var cut = RenderComponent<ExternalAppView>(parameters => parameters
                .Add(p => p.AppId, appId)
            );

            // Wait for loading to complete and error message to show
            await cut.WaitForStateAsync(() => cut.FindAll("fluent-message-box[intent='Error']").Any(), TimeSpan.FromSeconds(2));

            // Assert
            var errorBox = cut.Find("fluent-message-box[intent='Error']");
            Assert.Contains("Application not found", errorBox.Markup);
            Assert.NotNull(errorBox.Find("fluent-button")); // "Go to Dashboard" button
        }

        [Fact]
        public async Task ExternalAppView_GoToDashboardButton_NavigatesHome()
        {
            // Arrange
            var appId = 99;
            _mockAppService.Setup(s => s.GetByIdAsync(appId)).ReturnsAsync((ExternalApplicationDto?)null);
            var cut = RenderComponent<ExternalAppView>(parameters => parameters.Add(p => p.AppId, appId));
            await cut.WaitForStateAsync(() => cut.FindAll("fluent-message-box[intent='Error']").Any(), TimeSpan.FromSeconds(2));
            var button = cut.Find("fluent-message-box[intent='Error'] fluent-button");

            // Act
            await button.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            // Assert
            _mockNavigationManager.Verify(nav => nav.NavigateTo("/", false, false), Times.Once);
        }
    }
}
