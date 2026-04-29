using System.Diagnostics;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Layout;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.App.Extensions;
using TibiaHuntMaster.App.Services.Changelog;
using TibiaHuntMaster.App.Services.Map;
using TibiaHuntMaster.App.Services;
using TibiaHuntMaster.App.Services.Database;
using TibiaHuntMaster.App.Services.Diagnostics;
using TibiaHuntMaster.App.ViewModels;
using TibiaHuntMaster.App.Views;
using TibiaHuntMaster.Core.Abstractions.Map;
using TibiaHuntMaster.Infrastructure.Services.TibiaData;
using TibiaHuntMaster.Updater.Core.Abstractions;
using TibiaHuntMaster.Updater.Core.Models;

namespace TibiaHuntMaster.App
{
    internal sealed class App : Application
    {
        private const int DeferredUpdateReminderInterval = 10;
        private const int UpdateFailureHintInterval = 5;
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
            ServiceCollection collection = new();
            collection.AddTibiaHuntMasterServices();

            Services = collection.BuildServiceProvider();
            _logger = Services.GetRequiredService<ILoggerFactory>().CreateLogger<App>();
            _exceptionMonitor = Services.GetRequiredService<AppExceptionMonitor>();
            _exceptionMonitor.Start();

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
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                StartDesktopFlowSafe(desktop);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void StartDesktopFlowSafe(IClassicDesktopStyleApplicationLifetime desktop)
        {
            _ = ObserveTaskAsync(StartDesktopFlowAsync(desktop), nameof(StartDesktopFlowAsync));
        }

        private async Task StartDesktopFlowAsync(IClassicDesktopStyleApplicationLifetime desktop)
        {
            if(Services is null)
            {
                return;
            }

            bool continueStartup = await CheckForApplicationUpdatesAsync(desktop);
            if(!continueStartup)
            {
                return;
            }

            MainWindowViewModel mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
            MainWindow mainWindow = new()
            {
                DataContext = mainViewModel
            };

            desktop.MainWindow = mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
            mainWindow.Activate();

            if(TryGetUpdateCompletionDetails(out string? completedVersion, out string? releasePageUrl))
            {
                IChangelogService changelogService = Services.GetRequiredService<IChangelogService>();
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateCompletedWindow window = new(completedVersion!, releasePageUrl, changelogService);
                    window.Show();
                });
            }
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

        private async Task ObserveTaskAsync(Task task, string operationName)
        {
            try
            {
                await task;
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "{OperationName} failed.", operationName);
            }
        }

        private async Task<bool> CheckForApplicationUpdatesAsync(IClassicDesktopStyleApplicationLifetime desktop)
        {
            if(Services is null)
            {
                return true;
            }

            UserPreferencesService? preferencesService = Services.GetService<UserPreferencesService>();
            IUpdatePlanner? updatePlanner = Services.GetService<IUpdatePlanner>();
            if(updatePlanner is null)
            {
                _logger?.LogInformation("No update planner is registered. Continuing normal startup.");
                await HandleUpdateCheckFailureAsync(
                    preferencesService,
                    "Automatic app updates are currently unavailable because no update planner is registered.");
                return true;
            }

            string currentVersion = GetCurrentVersion();
            UpdateCheckResult? result = await updatePlanner.CheckForUpdateAsync(currentVersion);

            if(result is null)
            {
                await HandleUpdateCheckFailureAsync(
                    preferencesService,
                    "Automatic app updates are currently unavailable.");
                return true;
            }

            if (result.Status != UpdateCheckStatus.UpdateAvailable || result.UpdatePlan is null)
            {
                if(result.Status == UpdateCheckStatus.UpToDate)
                {
                    preferencesService?.ResetUpdateCheckFailureCounter();
                    preferencesService?.ClearDeferredUpdatePrompt();
                    return true;
                }

                if(result.Status == UpdateCheckStatus.UnsupportedPlatform)
                {
                    preferencesService?.ResetUpdateCheckFailureCounter();
                    _logger?.LogInformation("Update check skipped because the current platform is not supported.");
                    return true;
                }

                _logger?.LogWarning(
                    "Update check finished with status {Status}. Message: {ErrorMessage}",
                    result.Status,
                    result.ErrorMessage);

                await HandleUpdateCheckFailureAsync(
                    preferencesService,
                    result.ErrorMessage ?? "Automatic app updates are currently unavailable.");
                return true;
            }

            preferencesService?.ResetUpdateCheckFailureCounter();

            if(preferencesService is not null
               && !preferencesService.ShouldShowDeferredUpdatePrompt(
                   result.UpdatePlan.TargetVersion,
                   DeferredUpdateReminderInterval))
            {
                _logger?.LogInformation(
                    "Update reminder for version {TargetVersion} is deferred. Showing prompt again after {ReminderInterval} starts.",
                    result.UpdatePlan.TargetVersion,
                    DeferredUpdateReminderInterval);
                return true;
            }

            bool shouldUpdate = await ShowUpdatePromptAsync(result.UpdatePlan);
            if(!shouldUpdate)
            {
                preferencesService?.DeferUpdatePrompt(result.UpdatePlan.TargetVersion);
                _logger?.LogInformation(
                    "User skipped update from version {CurrentVersion} to {TargetVersion}.",
                    result.CurrentVersion,
                    result.UpdatePlan.TargetVersion);
                return true;
            }

            preferencesService?.ClearDeferredUpdatePrompt();

            if(Services.GetService<AppDataPaths>() is not AppDataPaths appDataPaths)
            {
                _logger?.LogError("AppDataPaths service is not available. Cannot download update.");
                await ShowInformationWindowAsync(
                    "Update failed",
                    "The update could not be downloaded because the application data path is unavailable.");
                return true;
            }

            UpdateDownloadWindow downloadWindow = new(result.UpdatePlan.TargetVersion);
            downloadWindow.Show();

            UpdateDownloadResult downloadResult;
            try
            {
                string appBaseDirectory = appDataPaths.BaseDirectory;
                IProgress<UpdateDownloadProgress> progress = new Progress<UpdateDownloadProgress>(value =>
                {
                    downloadWindow.UpdateProgress(value.BytesReceived, value.TotalBytes);
                });

                downloadResult = await updatePlanner.DownloadUpdateAsync(result.UpdatePlan, appBaseDirectory, progress);
            }
            finally
            {
                downloadWindow.Close();
            }

            if(downloadResult.Status != UpdateDownloadStatus.Succeeded || string.IsNullOrWhiteSpace(downloadResult.DownloadFilePath))
            {
                _logger?.LogError(
                    "Update download failed. Status: {Status}, ErrorMessage: {ErrorMessage}",
                    downloadResult.Status,
                    downloadResult.ErrorMessage);

                await ShowInformationWindowAsync(
                    "Update failed",
                    downloadResult.ErrorMessage ?? "The update package could not be downloaded.");
                return true;
            }

            if(!TryStartUpdater(downloadResult))
            {
                await ShowInformationWindowAsync(
                    "Updater missing",
                    "The update package was downloaded, but the updater executable could not be started.");
                return true;
            }

            _logger?.LogInformation(
                "Updater started successfully for target version {TargetVersion}. Shutting down application.",
                result.UpdatePlan.TargetVersion);

            desktop.Shutdown();
            return false;
        }

        private async Task HandleUpdateCheckFailureAsync(UserPreferencesService? preferencesService, string message)
        {
            int failureCount = preferencesService?.RegisterUpdateCheckFailure() ?? 0;

            if(failureCount <= 0)
            {
                return;
            }

            if(failureCount != 1 && failureCount % UpdateFailureHintInterval != 0)
            {
                return;
            }

            string hintMessage =
                $"Automatic app updates are currently unavailable. This reminder is shown every {UpdateFailureHintInterval} starts until the update check works again.\n\nDetails: {message}";

            await ShowInformationWindowAsync("Update check unavailable", hintMessage);
        }

        private static string GetCurrentVersion()
        {
            string? informational = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if(string.IsNullOrWhiteSpace(informational))
                return "0.0.0";

            // Strip build metadata suffix (e.g. "1.0.0+abc1234" → "1.0.0")
            int plusIndex = informational.IndexOf('+');
            return plusIndex >= 0 ? informational[..plusIndex] : informational;
        }

        private static Task<bool> ShowUpdatePromptAsync(UpdatePlan updatePlan)
        {
            UpdatePromptWindow window = new(updatePlan);
            return window.ShowAsync();
        }

        private static Task ShowInformationWindowAsync(string title, string message)
        {
            InformationWindow window = new(title, message);
            return window.ShowAsync();
        }

        private bool TryStartUpdater(UpdateDownloadResult downloadResult)
        {
            string? updaterPath = ResolveUpdaterExecutablePath();
            string? currentExecutablePath = Environment.ProcessPath;

            if(string.IsNullOrWhiteSpace(updaterPath) || !File.Exists(updaterPath))
            {
                _logger?.LogError("Updater executable not found at {UpdaterPath}.", updaterPath);
                return false;
            }

            if(string.IsNullOrWhiteSpace(currentExecutablePath))
            {
                _logger?.LogError("Current process path is not available. Cannot start updater.");
                return false;
            }

            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = updaterPath,
                    UseShellExecute = false,
                    WorkingDirectory = AppContext.BaseDirectory,
                };

                startInfo.ArgumentList.Add("--wait-for-pid");
                startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
                startInfo.ArgumentList.Add("--package");
                startInfo.ArgumentList.Add(downloadResult.DownloadFilePath!);
                startInfo.ArgumentList.Add("--restart-executable");
                startInfo.ArgumentList.Add(currentExecutablePath);
                startInfo.ArgumentList.Add("--update-completed-version");
                startInfo.ArgumentList.Add(downloadResult.UpdatePlan.TargetVersion);

                if(!string.IsNullOrWhiteSpace(downloadResult.UpdatePlan.ReleasePageUrl))
                {
                    startInfo.ArgumentList.Add("--update-completed-release-page-url");
                    startInfo.ArgumentList.Add(downloadResult.UpdatePlan.ReleasePageUrl!);
                }

                Process? process = Process.Start(startInfo);
                return process is not null;
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Failed to start updater executable.");
                return false;
            }
        }

        private static string ResolveUpdaterExecutablePath()
        {
            string executableName = OperatingSystem.IsWindows()
                ? "TibiaHuntMaster.Updater.exe"
                : "TibiaHuntMaster.Updater";

            return Path.Combine(AppContext.BaseDirectory, executableName);
        }

        private static bool TryGetUpdateCompletionDetails(out string? completedVersion, out string? releasePageUrl)
        {
            string[] args = Environment.GetCommandLineArgs();
            completedVersion = GetCommandLineValue(args, "--update-completed-version");
            releasePageUrl = GetCommandLineValue(args, "--update-completed-release-page-url");

            return !string.IsNullOrWhiteSpace(completedVersion);
        }

        private static string? GetCommandLineValue(string[] args, string argumentName)
        {
            for(int index = 0; index < args.Length - 1; index++)
            {
                if(string.Equals(args[index], argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    return args[index + 1];
                }
            }

            return null;
        }

        private sealed class UpdatePromptWindow : Window
        {
            private readonly TaskCompletionSource<bool> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public UpdatePromptWindow(UpdatePlan updatePlan)
            {
                Title = "Update available";
                Width = 440;
                SizeToContent = SizeToContent.Height;
                CanResize = false;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

                TextBlock headline = new()
                {
                    Text = $"A new version is available: {updatePlan.TargetVersion}",
                    FontSize = 18,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                };

                TextBlock details = new()
                {
                    Text = $"Current version: {updatePlan.CurrentVersion}\nPublished: {updatePlan.PublishedAtUtc:yyyy-MM-dd HH:mm} UTC\nDo you want to download and install the update now?",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                };

                Button laterButton = new()
                {
                    Content = "Later",
                    MinWidth = 100,
                };
                laterButton.Click += (_, _) => CloseWithResult(false);

                Button updateButton = new()
                {
                    Content = "Update now",
                    MinWidth = 120,
                };
                updateButton.Click += (_, _) => CloseWithResult(true);

                StackPanel buttonPanel = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children =
                    {
                        laterButton,
                        updateButton
                    }
                };

                Content = new Border
                {
                    Padding = new Thickness(20),
                    Child = new StackPanel
                    {
                        Spacing = 16,
                        Children =
                        {
                            headline,
                            details,
                            buttonPanel
                        }
                    }
                };

                Closed += (_, _) =>
                {
                    if(!_taskCompletionSource.Task.IsCompleted)
                    {
                        _taskCompletionSource.TrySetResult(false);
                    }
                };
            }

            public Task<bool> ShowAsync()
            {
                Show();
                Activate();
                return _taskCompletionSource.Task;
            }

            private void CloseWithResult(bool result)
            {
                _taskCompletionSource.TrySetResult(result);
                Close();
            }
        }

        private sealed class UpdateDownloadWindow : Window
        {
            private readonly ProgressBar _progressBar;
            private readonly TextBlock _statusText;

            public UpdateDownloadWindow(string targetVersion)
            {
                Title = "Downloading update";
                Width = 420;
                SizeToContent = SizeToContent.Height;
                CanResize = false;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

                _statusText = new TextBlock
                {
                    Text = "Waiting for download progress...",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                };

                _progressBar = new ProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    IsIndeterminate = true,
                    Height = 10,
                };

                Content = new Border
                {
                    Padding = new Thickness(20),
                    Child = new StackPanel
                    {
                        Spacing = 16,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"Downloading update {targetVersion}...",
                                FontSize = 18,
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            },
                            new TextBlock
                            {
                                Text = "Please wait while the update package is downloaded.",
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            },
                            _statusText,
                            _progressBar
                        }
                    }
                };
            }

            public void UpdateProgress(long bytesReceived, long? totalBytes)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if(totalBytes is > 0)
                    {
                        double percentage = Math.Clamp(bytesReceived * 100d / totalBytes.Value, 0, 100);
                        _progressBar.IsIndeterminate = false;
                        _progressBar.Value = percentage;
                        _statusText.Text = $"Downloaded {FormatByteSize(bytesReceived)} of {FormatByteSize(totalBytes.Value)} ({percentage:0.0}%).";
                        return;
                    }

                    _progressBar.IsIndeterminate = true;
                    _progressBar.Value = 0;
                    _statusText.Text = $"Downloaded {FormatByteSize(bytesReceived)}...";
                });
            }

            private static string FormatByteSize(long bytes)
            {
                if(bytes >= 1024L * 1024L * 1024L)
                {
                    return $"{bytes / (1024d * 1024d * 1024d):0.0} GB";
                }

                if(bytes >= 1024L * 1024L)
                {
                    return $"{bytes / (1024d * 1024d):0.0} MB";
                }

                if(bytes >= 1024L)
                {
                    return $"{bytes / 1024d:0.0} KB";
                }

                return $"{bytes} B";
            }
        }

        private sealed class InformationWindow : Window
        {
            private readonly TaskCompletionSource _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public InformationWindow(string title, string message)
            {
                Title = title;
                Width = 420;
                SizeToContent = SizeToContent.Height;
                CanResize = false;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

                Button okButton = new()
                {
                    Content = "OK",
                    MinWidth = 100,
                    HorizontalAlignment = HorizontalAlignment.Right,
                };
                okButton.Click += (_, _) => Close();

                Content = new Border
                {
                    Padding = new Thickness(20),
                    Child = new StackPanel
                    {
                        Spacing = 16,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = message,
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            },
                            okButton
                        }
                    }
                };

                Closed += (_, _) => _taskCompletionSource.TrySetResult();
            }

            public Task ShowAsync()
            {
                Show();
                Activate();
                return _taskCompletionSource.Task;
            }
        }

        private sealed class UpdateCompletedWindow : Window
        {
            private readonly StackPanel _changelogPanel;

            public UpdateCompletedWindow(string completedVersion, string? releasePageUrl, IChangelogService changelogService)
            {
                Title = $"Updated to {completedVersion}";
                Width = 600;
                Height = 540;
                CanResize = true;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

                _changelogPanel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 4, 0, 0) };
                _changelogPanel.Children.Add(new TextBlock
                {
                    Text = "Loading changelog...",
                    FontSize = 13,
                    Opacity = 0.6,
                });

                Button closeButton = new()
                {
                    Content = "Continue",
                    MinWidth = 100,
                };
                closeButton.Click += (_, _) => Close();

                StackPanel buttonPanel = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 12, 0, 0),
                    Children = { closeButton }
                };

                if(!string.IsNullOrWhiteSpace(releasePageUrl))
                {
                    Button githubButton = new()
                    {
                        Content = "View on GitHub",
                        MinWidth = 120,
                    };
                    githubButton.Click += (_, _) => OpenUrl(releasePageUrl!);
                    buttonPanel.Children.Insert(0, githubButton);
                }

                TextBlock headline = new()
                {
                    Text = $"What's new in {completedVersion}",
                    FontSize = 18,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                };

                DockPanel layout = new()
                {
                    Margin = new Thickness(20),
                    LastChildFill = true,
                };

                ScrollViewer scrollViewer = new()
                {
                    Content = _changelogPanel,
                    Margin = new Thickness(0, 8, 0, 0),
                };

                DockPanel.SetDock(headline, Dock.Top);
                DockPanel.SetDock(buttonPanel, Dock.Bottom);
                layout.Children.Add(headline);
                layout.Children.Add(buttonPanel);
                layout.Children.Add(scrollViewer);

                Content = layout;

                _ = LoadChangelogAsync(completedVersion, releasePageUrl, changelogService);
            }

            private async Task LoadChangelogAsync(string version, string? releasePageUrl, IChangelogService changelogService)
            {
                try
                {
                    string? markdown = await changelogService.GetChangelogSectionAsync(version, releasePageUrl);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _changelogPanel.Children.Clear();
                        if(markdown is null)
                        {
                            _changelogPanel.Children.Add(new TextBlock
                            {
                                Text = $"No changelog available for version {version}.",
                                FontSize = 13,
                                Opacity = 0.6,
                            });
                            return;
                        }
                        RenderMarkdown(markdown);
                    });
                }
                catch
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _changelogPanel.Children.Clear();
                        _changelogPanel.Children.Add(new TextBlock
                        {
                            Text = $"Could not load changelog for version {version}.",
                            FontSize = 13,
                            Opacity = 0.6,
                        });
                    });
                }
            }

            private void RenderMarkdown(string markdown)
            {
                string[] lines = markdown.Split('\n');
                bool lastWasEmpty = true;

                foreach(string rawLine in lines)
                {
                    string line = rawLine.TrimEnd();

                    if(string.IsNullOrEmpty(line))
                    {
                        lastWasEmpty = true;
                        continue;
                    }

                    TextBlock block;

                    if(line.StartsWith("## ", StringComparison.Ordinal))
                    {
                        block = new TextBlock
                        {
                            Text = line[3..].Trim(),
                            FontSize = 15,
                            FontWeight = Avalonia.Media.FontWeight.Bold,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Thickness(0, lastWasEmpty ? 6 : 2, 0, 4),
                        };
                    }
                    else if(line.StartsWith("### ", StringComparison.Ordinal))
                    {
                        block = new TextBlock
                        {
                            Text = line[4..].Trim(),
                            FontSize = 13,
                            FontWeight = Avalonia.Media.FontWeight.SemiBold,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Thickness(0, lastWasEmpty ? 8 : 2, 0, 2),
                        };
                    }
                    else if(line.StartsWith("- ", StringComparison.Ordinal))
                    {
                        block = new TextBlock
                        {
                            Text = "•  " + line[2..].Trim(),
                            FontSize = 13,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Thickness(14, 1, 0, 1),
                        };
                    }
                    else
                    {
                        block = new TextBlock
                        {
                            Text = line,
                            FontSize = 13,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Thickness(0, lastWasEmpty ? 4 : 0, 0, 0),
                        };
                    }

                    _changelogPanel.Children.Add(block);
                    lastWasEmpty = false;
                }
            }

            private static void OpenUrl(string url)
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
        }
    }
}
