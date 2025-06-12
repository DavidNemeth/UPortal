using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;
using UPortal.Components.Layout; // NavMenu is here
using UPortal.Dtos;
using UPortal.Services;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components; // For NavigationManager
using System; // For TimeSpan

namespace UPortal.Tests.Components.Layout
{
    public class NavMenuTests : TestContext
    {
        private readonly Mock<IExternalApplicationService> _mockAppService;
        private readonly Mock<NavigationManager> _mockNavigationManager;


        public NavMenuTests()
        {
            Services.AddFluentUIComponents();

            _mockAppService = new Mock<IExternalApplicationService>();

            _mockNavigationManager = new Mock<NavigationManager>();
            _mockNavigationManager.Setup(m => m.BaseUri).Returns("http://localhost/");
            _mockNavigationManager.Setup(m => m.Uri).Returns("http://localhost/"); // Current URI
            _mockNavigationManager.Setup(m => m.ToAbsoluteUri(It.IsAny<string>())).Returns<string>(uri => new System.Uri($"http://localhost/{uri.TrimStart('/')}"));
            _mockNavigationManager.Setup(m => m.NavigateTo(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()));


            Services.AddSingleton(_mockAppService.Object);
            Services.AddSingleton(_mockNavigationManager.Object); // Register mock NavMan
        }

        private List<ExternalApplicationDto> GetSampleApps() => new List<ExternalApplicationDto>
        {
            new ExternalApplicationDto { Id = 1, AppName = "App Alpha", AppUrl = "https://alpha.com", IconName = "@Icons.Regular.Size24.Home" },
            new ExternalApplicationDto { Id = 2, AppName = "App Beta", AppUrl = "https://beta.com", IconName = "@Icons.Regular.Size24.Link" }
        };

        [Fact]
        public async Task NavMenu_RendersLoadingState_ThenAppLinks()
        {
            // Arrange
            var sampleApps = GetSampleApps();
            _mockAppService.Setup(s => s.GetAllAsync()).ReturnsAsync(sampleApps);

            // Act
            var cut = RenderComponent<NavMenu>();

            // Assert: Initial loading state (FluentProgressRing inside accordion)
            Assert.NotNull(cut.Find("fluent-accordion fluent-progress-ring"));

            // Wait for apps to load and links to render
            await cut.WaitForStateAsync(() => cut.FindAll("fluent-nav-link[href^='/app/']").Count == sampleApps.Count, TimeSpan.FromSeconds(2));

            // Assert: App links are rendered
            var navLinks = cut.FindAll("fluent-nav-link[href^='/app/']");
            Assert.Equal(sampleApps.Count, navLinks.Count);

            foreach (var app in sampleApps)
            {
                var link = navLinks.FirstOrDefault(l => l.GetAttribute("href") == $"/app/{app.Id}");
                Assert.NotNull(link);
                Assert.Contains(app.AppName, link.TextContent);
                Assert.NotNull(link.QuerySelector("fluent-icon"));
            }
        }

        [Fact]
        public async Task NavMenu_ShowsNoAppsMessage_WhenNoAppsExist()
        {
            // Arrange
            _mockAppService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<ExternalApplicationDto>());

            // Act
            var cut = RenderComponent<NavMenu>();

            // Wait for loading to complete
            await cut.WaitForStateAsync(() => cut.FindAll("fluent-accordion fluent-progress-ring").Count == 0, TimeSpan.FromSeconds(2));

            // Assert
            var disabledLink = cut.Find("fluent-nav-link[disabled]");
            Assert.NotNull(disabledLink);
            Assert.Contains("No apps yet", disabledLink.TextContent);
        }

        [Fact]
        public async Task NavMenu_AccordionItem_RendersAndIsExpandedByDefault()
        {
            // Arrange
            _mockAppService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<ExternalApplicationDto>());

            // Act
            var cut = RenderComponent<NavMenu>();
            await cut.WaitForStateAsync(() => cut.FindAll("fluent-accordion fluent-progress-ring").Count == 0, TimeSpan.FromSeconds(2));

            // Assert
            var accordionItem = cut.Find("fluent-accordion-item");
            Assert.NotNull(accordionItem);
            Assert.True(accordionItem.Instance.Expanded);
            Assert.NotNull(cut.Find("fluent-accordion-item fluent-nav-link[disabled]"));
        }
    }
}
