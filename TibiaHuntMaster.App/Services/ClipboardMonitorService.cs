using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;

using TibiaHuntMaster.Infrastructure.Services.Analysis;

namespace TibiaHuntMaster.App.Services
{
    public sealed class ClipboardMonitorService(ILogDetectorService detector) : IDisposable
    {
        private readonly DispatcherTimer _timer = new()
        {
            Interval = TimeSpan.FromSeconds(3)
        };

        private string _lastText = string.Empty;
        private int _startRequests;
        private bool _isDisposed;

        public event Action<string, DetectedLogType>? LogDetected;

        public void Start()
        {
            if(_isDisposed)
            {
                return;
            }

            _startRequests++;
            if(_startRequests > 1)
            {
                return;
            }

            _timer.Tick += CheckClipboard;
            _timer.Start();
        }

        public void Stop()
        {
            if(_isDisposed)
            {
                return;
            }

            if(_startRequests > 0)
            {
                _startRequests--;
            }

            if(_startRequests > 0)
            {
                return;
            }

            _timer.Stop();
            _timer.Tick -= CheckClipboard;
        }

        public void Dispose()
        {
            if(_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _startRequests = 0;
            _timer.Stop();
            _timer.Tick -= CheckClipboard;
        }

        private async void CheckClipboard(object? sender, EventArgs e)
        {
            try
            {
                if(_isDisposed)
                {
                    return;
                }

                if(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    // CRASH FIX: Prüfen, ob MainWindow existiert und geladen ist
                    Window? window = desktop.MainWindow;
                    if(window is not { IsVisible: true })
                    {
                        return;
                    }

                    IClipboard? clipboard = window.Clipboard;
                    if(clipboard is null)
                    {
                        return;
                    }

                    // Zugriff auf Clipboard kann auf Linux (X11) fehlschlagen, wenn OS beschäftigt ist
                    string? text = null;
                    try
                    {
                        text = await clipboard.TryGetTextAsync();
                    }
                    catch
                    {
                        // Ignorieren - X11 Fehler sind oft temporär
                        return;
                    }

                    if(string.IsNullOrWhiteSpace(text))
                    {
                        return;
                    }
                    if(text == _lastText)
                    {
                        return;
                    }

                    DetectedLogType type = detector.DetectType(text);
                    if(type == DetectedLogType.None)
                    {
                        return;
                    }

                    _lastText = text;
                    LogDetected?.Invoke(text, type);
                }
            }
            catch (Exception)
            {
                // Globaler Catch für den Timer-Tick, damit die App nicht stirbt
            }
        }
    }
}
