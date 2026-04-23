using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.App.Extensions;
using TibiaHuntMaster.App.Services.Map;
using TibiaHuntMaster.App.Services;
using TibiaHuntMaster.App.Services.Database;
using TibiaHuntMaster.App.Services.Diagnostics;
using TibiaHuntMaster.App.ViewModels;
using TibiaHuntMaster.App.Views;
using TibiaHuntMaster.Core.Abstractions.Map;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Services.TibiaData;

namespace TibiaHuntMaster.App
{
    internal sealed class App : Application
    {
        private BoostedCreatureMonitor? _boostedCreatureMonitor;
        private ClipboardMonitorService? _clipboardMonitor;
        private AppExceptionMonitor? _exceptionMonitor;
        private ILogger<App>? _logger;

        /// <summary>
        ///     Zentraler Zugriff auf den DI-Container für die gesamte App.
        /// </summary>
        public IServiceProvider? Services { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // 1. DI Container aufbauen
            ServiceCollection collection = new();

            // Hier rufen wir unsere Extension-Methode auf, die ALLES registriert
            collection.AddTibiaHuntMasterServices();

            Services = collection.BuildServiceProvider();
            _logger = Services.GetRequiredService<ILoggerFactory>().CreateLogger<App>();
            _exceptionMonitor = Services.GetRequiredService<AppExceptionMonitor>();
            _exceptionMonitor.Start();

            // 2. Datenbank initialisieren (Migrationen anwenden & DB erstellen)
            DatabaseInitializationResult dbInit = new DatabaseInitializationService(
                Services,
                message => _logger.LogInformation("{Message}", message)).Initialize();
            if (!dbInit.Success)
            {
                DataStatusService status = Services.GetRequiredService<DataStatusService>();
                status.IsCriticalMissing = true;
                status.IsSyncing = false;
                status.IsInRetryDelay = false;
                status.StatusMessage = $"Database initialization failed: {dbInit.ErrorMessage}";
                _logger.LogCritical("Database initialization failed: {ErrorMessage}", dbInit.ErrorMessage);
            }

            // 3. Theme Service initialisieren und gespeicherte Theme-Präferenz laden
            ThemeService themeService = Services.GetRequiredService<ThemeService>();
            themeService.SetTheme(themeService.CurrentTheme);

            _boostedCreatureMonitor = Services.GetRequiredService<BoostedCreatureMonitor>();
            _boostedCreatureMonitor.Start();
            _clipboardMonitor = Services.GetRequiredService<ClipboardMonitorService>();

            _ = Task.Run(() =>
            {
                try
                {
                    _ = Services.GetRequiredService<IMinimapTileCatalog>();
                    _ = Services.GetRequiredService<IMinimapMarkerService>();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Minimap prewarm failed.");
                }
            });

            IMonsterImageCatalogService monsterImageCatalog = Services.GetRequiredService<IMonsterImageCatalogService>();
            _ = Task.Run(async () =>
            {
                try
                {
                    await monsterImageCatalog.EnsureCatalogAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Monster image catalog warm-up failed.");
                }
            });

            if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Exit += OnDesktopExit;

                // 3. MainWindowViewModel aus dem DI holen
                // Das stellt sicher, dass alle Abhängigkeiten (Services) korrekt injiziert werden.
                MainWindowViewModel mainViewModel = Services.GetRequiredService<MainWindowViewModel>();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            try
            {
                _clipboardMonitor?.Stop();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to stop clipboard monitor on exit.");
            }

            try
            {
                _boostedCreatureMonitor?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to dispose boosted monitor on exit.");
            }

            try
            {
                _exceptionMonitor?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to dispose app exception monitor on exit.");
            }

            if(Services is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to dispose service provider on exit.");
                }
            }
        }
    }
}
