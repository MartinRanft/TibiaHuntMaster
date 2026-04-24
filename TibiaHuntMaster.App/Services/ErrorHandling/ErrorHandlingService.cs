using Microsoft.Extensions.Logging;

namespace TibiaHuntMaster.App.Services.ErrorHandling
{
    /// <summary>
    ///     Implementation of centralized error handling service.
    /// </summary>
    public sealed class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;

        public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger;
        }

        public async Task HandleExceptionAsync(
            Exception exception,
            string? userMessage = null,
            ErrorSeverity severity = ErrorSeverity.Error,
            string? context = null)
        {
            // Log the exception
            LogException(exception, severity, context);

            // Raise event
            RaiseErrorEvent(exception, userMessage ?? exception.Message, severity, context);

            // Show notification if user message provided
            if (!string.IsNullOrEmpty(userMessage))
            {
                await ShowNotificationAsync(GetTitle(severity), userMessage, severity);
            }
        }

        public async Task HandleErrorAsync(
            string message,
            ErrorSeverity severity = ErrorSeverity.Error,
            string? context = null)
        {
            // Log the error
            LogMessage(message, severity, context);

            // Raise event
            RaiseErrorEvent(null, message, severity, context);

            // Show notification
            await ShowNotificationAsync(GetTitle(severity), message, severity);
        }

        public Task ShowNotificationAsync(
            string title,
            string message,
            ErrorSeverity severity = ErrorSeverity.Info)
        {
            // For now, just log - can be extended with actual UI notifications later
            // (e.g., toast notifications, dialog boxes, status bar messages)
            _logger.LogInformation("Notification [{Severity}] {Title}: {Message}", severity, title, message);

            // TODO: Implement actual UI notification system
            // This could integrate with:
            // - Avalonia notifications
            // - Dialog service
            // - Status bar service
            // - Toast notifications

            return Task.CompletedTask;
        }

        private void LogException(Exception exception, ErrorSeverity severity, string? context)
        {
            string contextInfo = string.IsNullOrEmpty(context) ? "" : $" [Context: {context}]";

            switch (severity)
            {
                case ErrorSeverity.Info:
                    _logger.LogInformation(exception, "Info{Context}", contextInfo);
                    break;
                case ErrorSeverity.Warning:
                    _logger.LogWarning(exception, "Warning{Context}", contextInfo);
                    break;
                case ErrorSeverity.Error:
                    _logger.LogError(exception, "Error{Context}", contextInfo);
                    break;
                case ErrorSeverity.Critical:
                    _logger.LogCritical(exception, "Critical Error{Context}", contextInfo);
                    break;
            }
        }

        private void LogMessage(string message, ErrorSeverity severity, string? context)
        {
            string contextInfo = string.IsNullOrEmpty(context) ? "" : $" [Context: {context}]";

            switch (severity)
            {
                case ErrorSeverity.Info:
                    _logger.LogInformation("{Message}{Context}", message, contextInfo);
                    break;
                case ErrorSeverity.Warning:
                    _logger.LogWarning("{Message}{Context}", message, contextInfo);
                    break;
                case ErrorSeverity.Error:
                    _logger.LogError("{Message}{Context}", message, contextInfo);
                    break;
                case ErrorSeverity.Critical:
                    _logger.LogCritical("{Message}{Context}", message, contextInfo);
                    break;
            }
        }

        private void RaiseErrorEvent(Exception? exception, string message, ErrorSeverity severity, string? context)
        {
            ErrorOccurred?.Invoke(this, new ErrorOccurredEventArgs
            {
                Exception = exception,
                Message = message,
                Severity = severity,
                Context = context,
                Timestamp = DateTime.Now
            });
        }

        private static string GetTitle(ErrorSeverity severity)
        {
            return severity switch
            {
                ErrorSeverity.Info => "Information",
                ErrorSeverity.Warning => "Warning",
                ErrorSeverity.Error => "Error",
                ErrorSeverity.Critical => "Critical Error",
                _ => "Notification"
            };
        }
    }
}
