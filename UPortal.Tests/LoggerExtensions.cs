// UPortal.Tests/LoggerExtensions.cs
using Microsoft.Extensions.Logging;
using Moq;
using System;

namespace UPortal.Tests
{
    public static class LoggerExtensions
    {
        public static void VerifyLogging<T>(
            this Mock<ILogger<T>> loggerMock,
            LogLevel expectedLevel,
            string expectedMessageSubstring,
            Times times,
            Exception? expectedException = null)
        {
            loggerMock.Verify(
                x => x.Log(
                    expectedLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessageSubstring)),
                    expectedException ?? It.IsAny<Exception>(), // Check for specific exception if provided, otherwise any exception
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                times);
        }

        // Overload without specific exception checking, if sometimes you only want to check level, message, and times
        public static void VerifyLogging<T>(
            this Mock<ILogger<T>> loggerMock,
            LogLevel expectedLevel,
            string expectedMessageSubstring,
            Times times)
        {
            loggerMock.Verify(
                x => x.Log(
                    expectedLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessageSubstring)),
                    It.IsAny<Exception>(), // Allow any exception for this overload
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                times);
        }

        // Overload for verifying any log at a specific level and times, without message or exception specifics
        public static void VerifyLogging<T>(
            this Mock<ILogger<T>> loggerMock,
            LogLevel expectedLevel,
            Times times)
        {
            loggerMock.Verify(
                x => x.Log(
                    expectedLevel,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                times);
        }
    }
}
