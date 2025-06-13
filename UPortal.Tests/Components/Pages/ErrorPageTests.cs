// UPortal.Tests/Components/Pages/ErrorPageTests.cs
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using UPortal.Tests; // For LoggerExtensions
using Microsoft.VisualStudio.TestTools.UnitTesting; // Using MSTest

namespace UPortal.Tests.Components.Pages
{
    // Define a testable version of the Error page's code-behind logic
    public class ErrorPageTestable
    {
        public IHttpContextAccessor HttpContextAccessor { get; set; }
        public ILogger<ErrorPageTestable> Logger { get; set; } // Use ILogger<ErrorPageTestable> for test

        public string ErrorId { get; private set; }
        public string ErrorMessage { get; private set; }

        public ErrorPageTestable(IHttpContextAccessor httpContextAccessor, ILogger<ErrorPageTestable> logger)
        {
            HttpContextAccessor = httpContextAccessor;
            Logger = logger;
            // Default message, similar to how it might be in the actual component
            ErrorMessage = "An error occurred while processing your request.";
        }

        public void OnInitialized() // Made public for testing
        {
            var exceptionHandlerFeature = HttpContextAccessor.HttpContext?.Features.Get<IExceptionHandlerFeature>();
            if (exceptionHandlerFeature?.Error != null)
            {
                ErrorId = System.Guid.NewGuid().ToString(); // Simulate ID generation
                ErrorMessage = "An unexpected error has occurred. Please try again later.";
                Logger.LogError(exceptionHandlerFeature.Error, "Unhandled exception caught by global error handler. Error ID: {ErrorId} Path: {Path}", ErrorId, exceptionHandlerFeature.Path);
            }
            else
            {
                ErrorMessage = "An error occurred, but further details are unavailable.";
                Logger.LogWarning("Error page visited without specific exception details. Path: {Path}", HttpContextAccessor.HttpContext?.Request.Path);
            }
        }
    }

    [TestClass]
    public class ErrorPageTests
    {
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private Mock<HttpContext> _mockHttpContext;
        private Mock<IFeatureCollection> _mockFeatures;
        private Mock<IExceptionHandlerFeature> _mockExceptionHandlerFeature;
        private Mock<ILogger<ErrorPageTestable>> _mockLogger; // Logger for the testable class
        private ErrorPageTestable _errorPage;

        [TestInitialize]
        public void Initialize()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockFeatures = new Mock<IFeatureCollection>();
            _mockExceptionHandlerFeature = new Mock<IExceptionHandlerFeature>();
            _mockLogger = new Mock<ILogger<ErrorPageTestable>>();

            _mockHttpContext.Setup(ctx => ctx.Features).Returns(_mockFeatures.Object);
            _mockHttpContextAccessor.Setup(acc => acc.HttpContext).Returns(_mockHttpContext.Object);

            // Setup mock request path for the no-exception scenario
            var mockHttpRequest = new Mock<HttpRequest>();
            mockHttpRequest.Setup(req => req.Path).Returns(new PathString("/Error"));
            _mockHttpContext.Setup(ctx => ctx.Request).Returns(mockHttpRequest.Object);


            _errorPage = new ErrorPageTestable(_mockHttpContextAccessor.Object, _mockLogger.Object);
        }

        [TestMethod]
        public void OnInitialized_ExceptionPresent_LogsErrorAndSetsProperties()
        {
            // Arrange
            var testException = new Exception("Test unhandled exception");
            _mockExceptionHandlerFeature.Setup(f => f.Error).Returns(testException);
            _mockExceptionHandlerFeature.Setup(f => f.Path).Returns("/some/error/path");
            _mockFeatures.Setup(f => f.Get<IExceptionHandlerFeature>()).Returns(_mockExceptionHandlerFeature.Object);

            // Act
            _errorPage.OnInitialized();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(_errorPage.ErrorId));
            Assert.AreEqual("An unexpected error has occurred. Please try again later.", _errorPage.ErrorMessage);
            _mockLogger.VerifyLogging(
                LogLevel.Error,
                $"Unhandled exception caught by global error handler. Error ID: {_errorPage.ErrorId} Path: /some/error/path",
                Times.Once(),
                expectedException: testException);
        }

        [TestMethod]
        public void OnInitialized_NoExceptionPresent_LogsWarningAndSetsDefaultMessage()
        {
            // Arrange
            // Ensure IExceptionHandlerFeature is null or its Error property is null
            _mockExceptionHandlerFeature.Setup(f => f.Error).Returns((Exception?)null);
            _mockFeatures.Setup(f => f.Get<IExceptionHandlerFeature>()).Returns(_mockExceptionHandlerFeature.Object);
            // Or alternatively, for a cleaner "feature not found" scenario:
            // _mockFeatures.Setup(f => f.Get<IExceptionHandlerFeature>()).Returns((IExceptionHandlerFeature?)null);


            // Act
            _errorPage.OnInitialized();

            // Assert
            Assert.IsTrue(string.IsNullOrEmpty(_errorPage.ErrorId)); // No ErrorId should be generated
            Assert.AreEqual("An error occurred, but further details are unavailable.", _errorPage.ErrorMessage);
            _mockLogger.VerifyLogging(
                LogLevel.Warning,
                "Error page visited without specific exception details. Path: /Error",
                Times.Once());
        }
    }
}
