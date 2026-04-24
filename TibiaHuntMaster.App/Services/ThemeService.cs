using Avalonia;
using Avalonia.Markup.Xaml.Styling;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TibiaHuntMaster.App.Services
{
    /// <summary>
    /// Available theme options
    /// </summary>
    public enum AppTheme
    {
        Dark,
        Light
    }

    /// <summary>
    /// Service for managing application theme switching
    /// </summary>
    public sealed class ThemeService
    {
        private const string DarkThemeUri = "avares://TibiaHuntMaster.App/Styles/Themes/DarkTheme.axaml";
        private const string LightThemeUri = "avares://TibiaHuntMaster.App/Styles/Themes/LightTheme.axaml";

        private readonly ILogger<ThemeService> _logger;
        private readonly UserPreferencesService? _preferencesService;
        private AppTheme _currentTheme = AppTheme.Dark;

        public ThemeService(UserPreferencesService? preferencesService = null, ILogger<ThemeService>? logger = null)
        {
            _preferencesService = preferencesService;
            _logger = logger ?? NullLogger<ThemeService>.Instance;

            // Load saved theme preference on startup
            if (_preferencesService != null)
            {
                _currentTheme = _preferencesService.GetThemePreference();
            }
        }

        /// <summary>
        /// Event raised when theme changes
        /// </summary>
        public event EventHandler<AppTheme>? ThemeChanged;

        /// <summary>
        /// Gets the current active theme
        /// </summary>
        public AppTheme CurrentTheme => _currentTheme;

        /// <summary>
        /// Sets the application theme
        /// </summary>
        /// <param name="theme">Theme to apply</param>
        public void SetTheme(AppTheme theme)
        {
            _currentTheme = theme;

            Application? app = Application.Current;
            if (app?.Resources == null)
            {
                _logger.LogWarning("Theme switch aborted because Application resources are not available");
                return;
            }

            string themeUri = theme == AppTheme.Dark ? DarkThemeUri : LightThemeUri;

            // Load the theme dictionary
            ResourceInclude themeDict = new ResourceInclude(new Uri(themeUri))
            {
                Source = new Uri(themeUri)
            };

            // Get all brush resource keys
            string[] brushKeys = new[]
            {
                "BrushBackground", "BrushSurface", "BrushSurfaceElevated", "BrushSurfaceHover",
                "BrushBorder", "BrushBorderHover", "BrushBorderLight",
                "BrushTextPrimary", "BrushTextSecondary", "BrushTextTertiary", "BrushTextDisabled", "BrushTextMuted",
                "BrushAccentGold", "BrushAccentBlue", "BrushAccentGreen", "BrushAccentRed", "BrushAccentPurple", "BrushAccentOrange",
                "BrushSuccess", "BrushWarning", "BrushError", "BrushInfo", "BrushOverlay"
            };

            int updated = 0;
            // Copy each brush from theme dictionary to app resources
            foreach (string key in brushKeys)
            {
                if (themeDict.TryGetResource(key, null, out object? resource))
                {
                    // Force update by removing and re-adding
                    if (app.Resources.ContainsKey(key))
                    {
                        app.Resources.Remove(key);
                    }
                    app.Resources.Add(key, resource);
                    updated++;
                }
            }

            _logger.LogDebug("Applied theme {Theme} from {ThemeUri}; updated {UpdatedResourceCount} resources", theme, themeUri, updated);

            // Save theme preference to disk
            _preferencesService?.SaveThemePreference(theme);

            ThemeChanged?.Invoke(this, theme);
        }

        /// <summary>
        /// Toggles between Dark and Light theme
        /// </summary>
        public void ToggleTheme()
        {
            AppTheme newTheme = _currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
            _logger.LogDebug("Toggling theme from {CurrentTheme} to {NewTheme}", _currentTheme, newTheme);
            SetTheme(newTheme);
        }
    }
}
