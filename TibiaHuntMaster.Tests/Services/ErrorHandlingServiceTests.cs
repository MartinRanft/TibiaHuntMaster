using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TibiaHuntMaster.App.Services.ErrorHandling;

namespace TibiaHuntMaster.Tests.Services
{
    public sealed class ErrorHandlingServiceTests
    {
        private readonly Mock<ILogger<ErrorHandlingService>> _loggerMock;

        public ErrorHandlingServiceTests()
        {
            _loggerMock = new Mock<ILogger<ErrorHandlingService>>();
        }

        [Fact]
        public void Constructor_ShouldInitializeService()
        {
            // Act
            ErrorHandlingService service = new ErrorHandlingService(_loggerMock.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public async Task HandleExceptionAsync_ShouldLogException()
        {
            // Arrange
            ErrorHandlingService service = new ErrorHandlingService(_loggerMock.Object);
            InvalidOperationException exception = new InvalidOperationException("Test exception");

            // Act
            await service.HandleExceptionAsync(exception, context: "TestContext");

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleExceptionAsync_ShouldRaiseErrorOccurredEvent()
        {
            // Arrange
            ErrorHandlingService service = new ErrorHandlingService(_loggerMock.Object);
            InvalidOperationException exception = new InvalidOperationException("Test exception");
            ErrorOccurredEventArgs? raisedEventArgs = null;

            service.ErrorOccurred += (sender, args) => { raisedEventArgs = args; };

            // Act
            await service.HandleExceptionAsync(exception, "User message", ErrorSeverity.Error, "TestContext");

            // Assert
            raisedEventArgs.Should().NotBeNull();
            raisedEventArgs!.Exception.Should().Be(exception);
            raisedEventArgs.Message.Should().Be("User message");
            raisedEventArgs.Severity.Should().Be(ErrorSeverity.Error);
            raisedEventArgs.Context.Should().Be("TestContext");
        }

        [Fact]
        public async Task HandleExceptionAsync_WithCriticalSeverity_ShouldLogCritical()
        {
            // Arrange
            ErrorHandlingService service = new ErrorHandlingService(_loggerMock.Object);
            Exception exception = new Exception("Critical error");

            // Act
            await service.HandleExceptionAsync(exception, severity: ErrorSeverity.Critical);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Critical,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleExceptionAsync_WithWarningSeverity_ShouldLogWarning()
        {
            // Arrange
            ErrorHandlingService service = new ErrorHandlingService(_loggerMock.Object);
            Exception exception = new Exception("Warning");

            // Act
            await service.HandleExceptionAsync(exception, severity: ErrorSeverity.Warning);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleErrorAsync_ShouldLogMessage()
        {
            // Arrange
            ErrorHandlingService service = new ErrorHandlingService(_loggerMock.Object);
            string errorMessage = "Something went wrong";

            // Act
            await service.HandleErrorAsync(errorMessage, ErrorSeverity.Error, "TestContext");

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleErrorAsync_ShouldRaiseErrorOccurredEvent()
        {
            // Arrange
            ErrorHandlingService service = new ErrorHandlingService(_loggerMock.Object);
            string errorMessage = "Something went wrong";
            ErrorOccurredEventArgs? raisedEventArgs = null;

            service.ErrorOccurred += (sender, args) => { raisedEventArgs = args; };

            // Act
            await service.HandleErrorAsync(errorMessage, ErrorSeverity.Warning, "TestContext");

            // Assert
            raisedEventArgs.Should().NotBeNull();
            raisedEventArgs!.Exception.Should().BeNull();
            raisedEventArgs.Message.Should().Be(errorMessage);
            raisedEventArgs.Severity.Should().Be(ErrorSeverity.Warning);
            raisedEventArgs.Context.Should().Be("TestContext");
        }

        [Fact]
        public async Task ShowNotificationAsync_ShouldLogNotification()
        {
            // Arrange
            ErrorHandlingService service = new ErrorHandlingService(_loggerMock.Object);

            // Act
            await service.ShowNotificationAsync("Test Title", "Test Message", ErrorSeverity.Info);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleExceptionAsync_WithoutUserMessage_ShouldNotShowNotification()
        {
            // Arrange
            ErrorHandlingService service = new ErrorHandlingService(_loggerMock.Object);
            Exception exception = new Exception("Test");

            // Act
            await service.HandleExceptionAsync(exception);

            // Assert
            // Only 1 log call for the exception, not 2 (no notification log)
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleExceptionAsync_WithUserMessage_ShouldShowNotification()
        {
            // Arrange
            ErrorHandlingService service = new ErrorHandlingService(_loggerMock.Object);
            Exception exception = new Exception("Test");

            // Act
            await service.HandleExceptionAsync(exception, userMessage: "User friendly message");

            // Assert
            // 2 log calls: 1 for exception, 1 for notification
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Exactly(2));
        }

        [Theory]
        [InlineData(ErrorSeverity.Info, LogLevel.Information)]
        [InlineData(ErrorSeverity.Warning, LogLevel.Warning)]
        [InlineData(ErrorSeverity.Error, LogLevel.Error)]
        [InlineData(ErrorSeverity.Critical, LogLevel.Critical)]
        public async Task HandleErrorAsync_ShouldLogAtCorrectLevel(ErrorSeverity severity, LogLevel expectedLogLevel)
        {
            // Arrange
            ErrorHandlingService service = new ErrorHandlingService(_loggerMock.Object);

            // Act
            await service.HandleErrorAsync("Test message", severity);

            // Assert
            // HandleErrorAsync logs twice: once for the error message, once for the notification
            // We verify that at least one log call uses the expected level
            _loggerMock.Verify(
                x => x.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}
