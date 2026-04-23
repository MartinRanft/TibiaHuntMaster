using System;
using System.Threading.Tasks;

using Avalonia.Threading;

using Microsoft.Extensions.Logging;

namespace TibiaHuntMaster.App.Services.Diagnostics
{
    public sealed class AppExceptionMonitor : IDisposable
    {
        private readonly IDiagnosticsService _diagnosticsService;
        private readonly ILogger<AppExceptionMonitor> _logger;
        private bool _isStarted;

        public AppExceptionMonitor(IDiagnosticsService diagnosticsService, ILogger<AppExceptionMonitor> logger)
        {
            _diagnosticsService = diagnosticsService;
            _logger = logger;
        }

        public void Start()
        {
            if (_isStarted)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
            Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
            _isStarted = true;
        }

        public void Dispose()
        {
            if (!_isStarted)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnTaskSchedulerUnobservedTaskException;
            Dispatcher.UIThread.UnhandledException -= OnDispatcherUnhandledException;
            _isStarted = false;
        }

        private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _diagnosticsService.CaptureExceptionReport(e.Exception, "Dispatcher.UIThread.UnhandledException", isTerminating: false);
            _logger.LogError(e.Exception, "Unhandled UI exception captured by Avalonia dispatcher.");
        }

        private void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _diagnosticsService.CaptureExceptionReport(e.Exception, "TaskScheduler.UnobservedTaskException", isTerminating: false);
            _logger.LogError(e.Exception, "Unobserved task exception captured in background task.");
            e.SetObserved();
        }

        private void OnAppDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception
                                  ?? new Exception($"Non-exception object thrown: {e.ExceptionObject}");

            _diagnosticsService.CaptureExceptionReport(exception, "AppDomain.CurrentDomain.UnhandledException", e.IsTerminating);
            _logger.LogCritical(exception, "Unhandled app-domain exception captured. IsTerminating={IsTerminating}", e.IsTerminating);
        }
    }
}
