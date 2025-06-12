using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;
using UPortal.Components.Dialogs;
using UPortal.Dtos;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms; // Required for EditContext

namespace UPortal.Tests.Components.Dialogs
{
    public class AddExternalApplicationDialogTests : TestContext
    {
        private readonly Mock<FluentDialog> _mockDialog;
        private readonly ExternalApplicationDto _testDto;

        public AddExternalApplicationDialogTests()
        {
            // Register FluentUI services
            Services.AddFluentUIComponents(); // This registers standard FluentUI services

            _mockDialog = new Mock<FluentDialog>();
            _testDto = new ExternalApplicationDto(); // Default DTO for tests

            // Mock the Dialog instance parameters if needed by the component
            var mockDialogInstance = new DialogInstance(new DialogParameters()
            {
                Title = "Test Dialog",
                Content = _testDto // Pass the DTO to the dialog
            });
            _mockDialog.Setup(d => d.Instance).Returns(mockDialogInstance);

        }

        private IRenderedComponent<AddExternalApplicationDialog> RenderDialog(ExternalApplicationDto? content = null)
        {
            content ??= new ExternalApplicationDto(); // Use a new DTO if none provided
            // Ensure the mockDialog's Instance.Parameters.Content is updated if a new 'content' is provided.
            // This is important because the component's OnInitialized uses Dialog.Instance.Parameters.Content if its own Content parameter is null.
            // However, our component initializes Content = new() if Parameter is not set, and it's always set by RenderDialog.
            // The critical part is that Dialog.Instance.Parameters.Title is used.
            var parameters = new DialogParameters() { Title = "Test Dialog" };
            if (_mockDialog.Object.Instance != null) // Guard against null Instance if setup changes
            {
                parameters = _mockDialog.Object.Instance.Parameters;
            }
             // Update the Content within the existing DialogParameters if possible, or recreate.
            parameters.Content = content; // Ensure the DTO is part of the dialog instance parameters.
             _mockDialog.Setup(d => d.Instance).Returns(new DialogInstance(parameters));


            return RenderComponent<AddExternalApplicationDialog>(parameters => parameters
                .AddCascadingValue(_mockDialog.Object) // Provide the mocked FluentDialog as a cascading parameter
                .Add(p => p.Content, content) // Pass the DTO via the Content parameter
            );
        }

        [Fact]
        public void Dialog_RendersCorrectly_WithInitialState()
        {
            // Arrange & Act
            var cut = RenderDialog();

            // Assert
            Assert.NotNull(cut.Find("fluent-text-field[label='App Name']"));
            Assert.NotNull(cut.Find("fluent-text-field[label='App URL']"));
            Assert.NotNull(cut.Find("fluent-select[placeholder='Select an icon']"));
            Assert.NotNull(cut.Find("fluent-button[appearance='Neutral']")); // Cancel
            Assert.NotNull(cut.Find("fluent-button[appearance='Accent']")); // Save
        }

        [Fact]
        public void Dialog_AppNameValidation_Required()
        {
            // Arrange
            var dto = new ExternalApplicationDto { AppUrl = "https://example.com", IconName = "@Icons.Regular.Size24.Home" };
            var cut = RenderDialog(dto);
            var saveButton = cut.Find("fluent-button[appearance='Accent']"); // Save button

            // Act: Try to save without AppName
            var editContext = cut.Instance.GetType().GetField("_editContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cut.Instance) as EditContext;
            Assert.NotNull(editContext);
            Assert.False(editContext.Validate());
            Assert.True(saveButton.HasAttribute("disabled"));

            // Act: Set AppName
            cut.Find("fluent-text-field[label='App Name'] input").Change("Test App");
            cut.WaitForState(() => editContext.Validate()); // Ensure validation re-runs and state updates

            // Assert
            Assert.True(editContext.Validate());
            Assert.False(saveButton.HasAttribute("disabled"));
        }

        [Fact]
        public void Dialog_AppUrlValidation_RequiredAndFormat()
        {
            // Arrange
            var dto = new ExternalApplicationDto { AppName = "Test App", IconName = "@Icons.Regular.Size24.Home" };
            var cut = RenderDialog(dto);
            var saveButton = cut.Find("fluent-button[appearance='Accent']");
            var editContext = cut.Instance.GetType().GetField("_editContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cut.Instance) as EditContext;
            Assert.NotNull(editContext);

            // Assert: Initially invalid because URL is empty
            Assert.False(editContext.Validate());
            Assert.True(saveButton.HasAttribute("disabled"));

            // Act: Set invalid URL
            cut.Find("fluent-text-field[label='App URL'] input").Change("invalid-url");
            cut.WaitForState(() => !editContext.Validate());


            // Assert: Still invalid
            Assert.False(editContext.Validate());
            Assert.Contains(editContext.GetValidationMessages(FieldIdentifier.Create(() => dto.AppUrl)), msg => msg.Contains("Invalid URL format"));
            Assert.True(saveButton.HasAttribute("disabled"));

            // Act: Set valid URL
            cut.Find("fluent-text-field[label='App URL'] input").Change("https://valid.com");
            cut.WaitForState(() => editContext.Validate());

            // Assert: Now valid
            Assert.True(editContext.Validate());
            Assert.False(saveButton.HasAttribute("disabled"));
        }

        [Fact]
        public void Dialog_IconValidation_Required()
        {
            // Arrange
            var dto = new ExternalApplicationDto { AppName = "Test App", AppUrl = "https://example.com" };
            var cut = RenderDialog(dto);
            var saveButton = cut.Find("fluent-button[appearance='Accent']");
            var editContext = cut.Instance.GetType().GetField("_editContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cut.Instance) as EditContext;
            Assert.NotNull(editContext);

            // Assert: Initially invalid because IconName is empty
            Assert.False(editContext.Validate());
            Assert.True(saveButton.HasAttribute("disabled"));

            // Act: Select an icon
            // Reflect directly into _selectedIconFullName and call HandleIconSelection
            var iconToSelect = cut.Instance._availableIcons.First(); // Select the first available icon
            cut.Instance.GetType().GetField("_selectedIconFullName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cut.Instance, iconToSelect.FullName);
            cut.Instance.HandleIconSelection(); // This updates Content.IconName and calls StateHasChanged
            cut.WaitForState(() => editContext.Validate());


            // Assert: Now valid
            Assert.True(editContext.Validate());
            Assert.False(saveButton.HasAttribute("disabled"));
        }

        [Fact]
        public void Dialog_IconSelection_UpdatesPreview()
        {
            // Arrange
            var cut = RenderDialog();

            // Assert: No preview initially
            Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find("fluent-icon"));

            // Act: Simulate selecting an icon.
            var iconToSelect = cut.Instance._availableIcons.First(i => i.Name == "Home");
            cut.Instance.GetType().GetField("_selectedIconFullName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cut.Instance, iconToSelect.FullName);
            cut.Instance.HandleIconSelection();
            cut.WaitForState(() => !string.IsNullOrEmpty(cut.Instance.Content.IconName) && cut.FindAll("fluent-icon").Any());


            // Assert: Preview for "Home" icon is now visible
            // The Value of FluentIcon is an Icon instance, not a string. We check for presence.
            var iconElement = cut.Find("fluent-icon");
            Assert.NotNull(iconElement);
            // To check the specific icon, you might need to inspect properties of the rendered FluentIcon,
            // or trust that if any fluent-icon is rendered here, it's the preview.
            // A more specific check would require knowledge of how FluentIcon renders its 'Value'.
            // For instance, if it adds an attribute like 'icon-name':
            // Assert.Equal(iconToSelect.IconInstance.GetType().Name, iconElement.GetAttribute("icon-name"));
        }

        [Fact]
        public async Task Dialog_SaveButton_ClosesDialogWithValidData()
        {
            // Arrange
            var dto = new ExternalApplicationDto
            {
                AppName = "My Test App",
                AppUrl = "https://mytest.com",
                // IconName will be set by simulating selection for full coverage
            };
            var cut = RenderDialog(dto);
            var editContext = cut.Instance.GetType().GetField("_editContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cut.Instance) as EditContext;
            Assert.NotNull(editContext);

            // Select an icon to make the form fully valid
            var iconToSelect = cut.Instance._availableIcons.First(i => i.Name == "Link");
            cut.Instance.GetType().GetField("_selectedIconFullName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cut.Instance, iconToSelect.FullName);
            cut.Instance.HandleIconSelection();
            cut.WaitForState(() => editContext.Validate());


            Assert.True(editContext.Validate(), "Form should be valid after setting all fields.");
            var saveButton = cut.Find("fluent-button[appearance='Accent']");
            Assert.False(saveButton.HasAttribute("disabled"), "Save button should be enabled with valid DTO.");

            // Act
            await cut.InvokeAsync(() => saveButton.Click());

            // Assert
            _mockDialog.Verify(d => d.CloseAsync(It.Is<ExternalApplicationDto>(val =>
                val.AppName == dto.AppName &&
                val.AppUrl == dto.AppUrl &&
                val.IconName == iconToSelect.FullName //Ensure IconName from selection is used
            )), Times.Once);
        }

        [Fact]
        public async Task Dialog_CancelButton_ClosesDialog()
        {
            // Arrange
            var cut = RenderDialog();
            var cancelButton = cut.Find("fluent-button[appearance='Neutral']");

            // Act
            await cut.InvokeAsync(() => cancelButton.Click());

            // Assert
            _mockDialog.Verify(d => d.CancelAsync(), Times.Once);
        }
    }
}
