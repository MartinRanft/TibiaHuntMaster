using System;
using System.Threading.Tasks;

namespace TibiaHuntMaster.App.Services.ErrorHandling
{
    /// <summary>
    ///     Severity level of an error.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        ///     Informational message - no action required.
        /// </summary>
        Info,

        /// <summary>
        ///     Warning - something unexpected happened but the app can continue.
        /// </summary>
        Warning,

        /// <summary>
        ///     Error - an operation failed but the app is still functional.
        /// </summary>
        Error,

        /// <summary>
        ///     Critical - a fatal error that may require app restart.
        /// </summary>
        Critical
    }

    /// <summary>
    ///     Service for centralized error handling and user notifications.
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        ///     Handles an exception with optional user message.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="userMessage">User-friendly message to display (optional).</param>
        /// <param name="severity">Severity level of the error.</param>
        /// <param name="context">Additional context about where the error occurred.</param>
        Task HandleExceptionAsync(
            Exception exception,
            string? userMessage = null,
            ErrorSeverity severity = ErrorSeverity.Error,
            string? context = null);

        /// <summary>
        ///     Handles an error message without an exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="severity">Severity level of the error.</param>
        /// <param name="context">Additional context about where the error occurred.</param>
        Task HandleErrorAsync(
            string message,
            ErrorSeverity severity = ErrorSeverity.Error,
            string? context = null);

        /// <summary>
        ///     Shows a notification to the user.
        /// </summary>
        /// <param name="title">Title of the notification.</param>
        /// <param name="message">Message content.</param>
        /// <param name="severity">Severity level.</param>
        Task ShowNotificationAsync(
            string title,
            string message,
            ErrorSeverity severity = ErrorSeverity.Info);

        /// <summary>
        ///     Event raised when a new error occurs.
        /// </summary>
        event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;
    }

    /// <summary>
    ///     Event args for error occurred event.
    /// </summary>
    public sealed class ErrorOccurredEventArgs : EventArgs
    {
        public Exception? Exception { get; init; }
        public string Message { get; init; } = string.Empty;
        public ErrorSeverity Severity { get; init; }
        public string? Context { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.Now;
    }
}
