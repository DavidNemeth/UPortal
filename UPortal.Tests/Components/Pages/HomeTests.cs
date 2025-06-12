using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;
using UPortal.Components.Dialogs;
using UPortal.Components.Pages;
using UPortal.Dtos;
using UPortal.Services;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components; // For NavigationManager

namespace UPortal.Tests.Components.Pages
{
    public class HomeTests : TestContext
    {
        private readonly Mock<IExternalApplicationService> _mockAppService;
        private readonly Mock<IDialogService> _mockDialogService;
        private readonly Mock<IToastService> _mockToastService;
        private readonly Mock<NavigationManager> _mockNavigationManager;

        public HomeTests()
        {
            Services.AddFluentUIComponents(); // Essential for Fluent UI components

            _mockAppService = new Mock<IExternalApplicationService>();
            _mockDialogService = new Mock<IDialogService>();
            _mockToastService = new Mock<IToastService>();

            // Setup a mock NavigationManager
            _mockNavigationManager = new Mock<NavigationManager>();
            _mockNavigationManager.Setup(m => m.NavigateTo(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()));
            _mockNavigationManager.Setup(m => m.BaseUri).Returns("http://localhost/"); // Required by bUnit
            _mockNavigationManager.Setup(m => m.Uri).Returns("http://localhost/");


            Services.AddSingleton(_mockAppService.Object);
            Services.AddSingleton(_mockDialogService.Object);
            Services.AddSingleton(_mockToastService.Object);
            Services.AddSingleton(_mockNavigationManager.Object); // Register as the NavigationManager
        }

        private List<ExternalApplicationDto> GetSampleApps() => new List<ExternalApplicationDto>
        {
            new ExternalApplicationDto { Id = 1, AppName = "App One", AppUrl = "https://one.com", IconName = "@Icons.Regular.Size24.Home" },
            new ExternalApplicationDto { Id = 2, AppName = "App Two", AppUrl = "https://two.com", IconName = "@Icons.Regular.Size24.Link" }
        };

        [Fact]
        public async Task Home_RendersLoadingState_ThenApplications()
        {
            // Arrange
            var sampleApps = GetSampleApps();
            _mockAppService.Setup(s => s.GetAllAsync()).ReturnsAsync(sampleApps);

            // Act
            var cut = RenderComponent<Home>();

            // Assert: Initial loading state (if any specific element exists for it)
            // For Home.razor, it shows "Loading applications..." and a progress ring
            Assert.NotNull(cut.Find("fluent-progress-ring"));
            Assert.Contains("<em>Loading applications...</em>", cut.Markup);

            // Wait for the component to update after data loading
            await cut.WaitForStateAsync(() => cut.FindAll("fluent-card").Count == sampleApps.Count, timeout: System.TimeSpan.FromSeconds(2));

            // Assert: Applications are rendered
            Assert.Equal(sampleApps.Count, cut.FindAll("fluent-card").Count);
            foreach (var app in sampleApps)
            {
                Assert.Contains(app.AppName, cut.Markup);
                // Check for icon rendering indirectly by looking for a fluent-icon within a card.
                // Specific icon check is harder without more detailed markup structure.
                var card = cut.FindAll("fluent-card").First(c => c.ToMarkup().Contains(app.AppName));
                Assert.NotNull(card.QuerySelector("fluent-icon"));
            }
            Assert.NotNull(cut.Find("fluent-button[title='Add External Application']")); // FAB
        }

        [Fact]
        public async Task Home_RendersNoApplicationsMessage_WhenNoAppsExist()
        {
            // Arrange
            _mockAppService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<ExternalApplicationDto>());

            // Act
            var cut = RenderComponent<Home>();
             await cut.WaitForStateAsync(() => cut.FindAll("fluent-progress-ring").Count == 0, timeout: System.TimeSpan.FromSeconds(2));


            // Assert
            Assert.Contains("No external applications have been added yet.", cut.Markup);
        }

        [Fact]
        public async Task Home_FABClick_OpensAddApplicationDialog()
        {
            // Arrange
            _mockAppService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<ExternalApplicationDto>());
            var cut = RenderComponent<Home>();
            await cut.WaitForStateAsync(() => cut.FindAll("fluent-progress-ring").Count == 0, timeout: System.TimeSpan.FromSeconds(2));


            var fab = cut.Find("fluent-button[title='Add External Application']");

            // Mock DialogService ShowDialogAsync to simulate dialog interaction
            var newAppDto = new ExternalApplicationDto { Id = 3, AppName = "New App", AppUrl = "https://new.com", IconName = "TestIcon" };
            var mockDialogReference = new Mock<IDialogReference>();
            mockDialogReference.Setup(r => r.Result).ReturnsAsync(DialogResult.Ok(newAppDto)); // Simulate Save

            _mockDialogService.Setup(ds => ds.ShowDialogAsync<AddExternalApplicationDialog>(It.IsAny<DialogParameters>()))
                .ReturnsAsync(mockDialogReference.Object);

            // Act
            await fab.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await cut.WaitForStateAsync(() => _mockAppService.Invocations.Any(inv => inv.Method.Name == "AddAsync"), timeout: System.TimeSpan.FromSeconds(2));


            // Assert
            _mockDialogService.Verify(ds => ds.ShowDialogAsync<AddExternalApplicationDialog>(It.Is<DialogParameters>(dp =>
                dp.Title == "Add New External Application" &&
                dp.Get<object>("Content") is ExternalApplicationDto // Check that a DTO is passed as content
            )), Times.Once);

            // Verify that AddAsync was called after dialog "Save"
            _mockAppService.Verify(s => s.AddAsync(It.Is<ExternalApplicationDto>(dto => dto.AppName == newAppDto.AppName)), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess("Application added successfully."), Times.Once);
        }

        [Fact]
        public async Task Home_FABClick_DialogCancelled_DoesNotAddApplication()
        {
            // Arrange
            _mockAppService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<ExternalApplicationDto>());
            var cut = RenderComponent<Home>();
            await cut.WaitForStateAsync(() => cut.FindAll("fluent-progress-ring").Count == 0);

            var fab = cut.Find("fluent-button[title='Add External Application']");

            var mockDialogReference = new Mock<IDialogReference>();
            mockDialogReference.Setup(r => r.Result).ReturnsAsync(DialogResult.Cancel()); // Simulate Cancel

            _mockDialogService.Setup(ds => ds.ShowDialogAsync<AddExternalApplicationDialog>(It.IsAny<DialogParameters>()))
                .ReturnsAsync(mockDialogReference.Object);

            // Act
            await fab.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
             await cut.WaitForStateAsync(() => _mockDialogService.Invocations.Count > 0, timeout: System.TimeSpan.FromSeconds(2));


            // Assert
            _mockDialogService.Verify(ds => ds.ShowDialogAsync<AddExternalApplicationDialog>(It.IsAny<DialogParameters>()), Times.Once);
            _mockAppService.Verify(s => s.AddAsync(It.IsAny<ExternalApplicationDto>()), Times.Never); // AddAsync should not be called
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Never);
        }


        [Fact]
        public async Task Home_DeleteApplication_Successful()
        {
            // Arrange
            var apps = GetSampleApps();
            var appToDelete = apps.First();
            _mockAppService.Setup(s => s.GetAllAsync()).ReturnsAsync(apps);

            var cut = RenderComponent<Home>();
            await cut.WaitForStateAsync(() => cut.FindAll("fluent-card").Count == apps.Count);

            // Mock confirmation dialog to return "confirmed" (not cancelled)
            var mockDialogReference = new Mock<IDialogReference>();
            mockDialogReference.Setup(r => r.Result).ReturnsAsync(DialogResult.Ok<object>(null!)); // Simulate "Yes, delete" (result not cancelled)

            _mockDialogService.Setup(ds => ds.ShowConfirmationAsync(
                    It.Is<string>(s => s.Contains(appToDelete.AppName)), // Check if message contains app name
                    "Yes, delete", "No, cancel", "Confirm Deletion"))
                .ReturnsAsync(mockDialogReference.Object);

            // Find the delete button for the specific app
            // This assumes each card contains the app name and then a delete button
            var appCard = cut.FindAll("fluent-card").First(card => card.ToMarkup().Contains(appToDelete.AppName));
            var deleteButton = appCard.Find("fluent-button[title^='Delete']"); // title starts with "Delete"

            // Act
            await deleteButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await cut.WaitForStateAsync(() => _mockAppService.Invocations.Any(inv => inv.Method.Name == "DeleteAsync"), timeout: System.TimeSpan.FromSeconds(2));


            // Assert
            _mockDialogService.Verify(ds => ds.ShowConfirmationAsync(
                $"Are you sure you want to delete '{appToDelete.AppName}'?", "Yes, delete", "No, cancel", "Confirm Deletion"), Times.Once);
            _mockAppService.Verify(s => s.DeleteAsync(appToDelete.Id), Times.Once);
            _mockToastService.Verify(t => t.ShowSuccess("Application deleted successfully."), Times.Once);
        }

        [Fact]
        public async Task Home_DeleteApplication_Cancelled()
        {
            // Arrange
            var apps = GetSampleApps();
            var appToDelete = apps.First();
            _mockAppService.Setup(s => s.GetAllAsync()).ReturnsAsync(apps);

            var cut = RenderComponent<Home>();
            await cut.WaitForStateAsync(() => cut.FindAll("fluent-card").Count == apps.Count);

            // Mock confirmation dialog to return "cancelled"
            var mockDialogReference = new Mock<IDialogReference>();
            mockDialogReference.Setup(r => r.Result).ReturnsAsync(DialogResult.Cancel()); // Simulate "No, cancel"

            _mockDialogService.Setup(ds => ds.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockDialogReference.Object);

            var appCard = cut.FindAll("fluent-card").First(card => card.ToMarkup().Contains(appToDelete.AppName));
            var deleteButton = appCard.Find("fluent-button[title^='Delete']");

            // Act
            await deleteButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await cut.WaitForStateAsync(() => _mockDialogService.Invocations.Any(inv => inv.Method.Name == "ShowConfirmationAsync"), timeout: System.TimeSpan.FromSeconds(2));


            // Assert
            _mockDialogService.Verify(ds => ds.ShowConfirmationAsync(
                $"Are you sure you want to delete '{appToDelete.AppName}'?", "Yes, delete", "No, cancel", "Confirm Deletion"), Times.Once);
            _mockAppService.Verify(s => s.DeleteAsync(appToDelete.Id), Times.Never);
            _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Home_ClickApplicationTile_NavigatesToAppView()
        {
            // Arrange
            var apps = GetSampleApps();
            var appToClick = apps.First();
            _mockAppService.Setup(s => s.GetAllAsync()).ReturnsAsync(apps);

            var cut = RenderComponent<Home>();
            await cut.WaitForStateAsync(() => cut.FindAll("fluent-card").Count == apps.Count);

            var appCard = cut.FindAll("fluent-card").First(card => card.ToMarkup().Contains(appToClick.AppName));

            // Act
            await appCard.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            // Assert
            _mockNavigationManager.Verify(nav => nav.NavigateTo($"/app/{appToClick.Id}", false, false), Times.Once);
        }
    }
}
