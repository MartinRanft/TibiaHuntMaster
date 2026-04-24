using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Text.Json;

namespace TibiaHuntMaster.App.Services.Localization
{
    /// <summary>
    ///     Service for managing application localization and language switching.
    /// </summary>
    public interface ILocalizationService : INotifyPropertyChanged
    {
        /// <summary>
        ///     Gets the current culture.
        /// </summary>
        CultureInfo CurrentCulture { get; }

        /// <summary>
        ///     Gets a localized string by key.
        /// </summary>
        /// <param name="key">Resource key.</param>
        /// <returns>Localized string.</returns>
        string this[string key] { get; }

        /// <summary>
        ///     Changes the current language.
        /// </summary>
        /// <param name="cultureCode">Culture code (e.g., "en", "de").</param>
        void ChangeLanguage(string cultureCode);

        /// <summary>
        ///     Gets available languages.
        /// </summary>
        /// <returns>List of supported culture codes.</returns>
        List<string> GetAvailableLanguages();
    }

    public sealed class LocalizationService : ILocalizationService
    {
        private readonly ResourceManager _resourceManager;
        private readonly UserPreferencesService? _preferencesService;
        private CultureInfo _currentCulture;

        public event PropertyChangedEventHandler? PropertyChanged;

        public LocalizationService(UserPreferencesService? preferencesService = null)
        {
            _preferencesService = preferencesService;
            _resourceManager = new ResourceManager(
                "TibiaHuntMaster.App.Resources.Localization.Strings",
                typeof(LocalizationService).Assembly);

            string? savedLanguage = _preferencesService?.GetLanguagePreference();
            if (string.IsNullOrWhiteSpace(savedLanguage))
            {
                savedLanguage = LoadLegacySavedLanguage();
                if (!string.IsNullOrWhiteSpace(savedLanguage))
                {
                    _preferencesService?.SaveLanguagePreference(savedLanguage);
                }
            }

            _currentCulture = TryCreateCulture(savedLanguage) ?? new CultureInfo("en");

            // Fallback to English if culture not supported
            if (!GetAvailableLanguages().Contains(_currentCulture.TwoLetterISOLanguageName))
            {
                _currentCulture = new CultureInfo("en");
            }
        }

        public CultureInfo CurrentCulture => _currentCulture;

        public string this[string key]
        {
            get
            {
                try
                {
                    string? value = _resourceManager.GetString(key, _currentCulture);
                    return value ?? $"[{key}]";
                }
                catch
                {
                    return $"[{key}]";
                }
            }
        }

        public void ChangeLanguage(string cultureCode)
        {
            CultureInfo newCulture = new CultureInfo(cultureCode);
            if (_currentCulture.TwoLetterISOLanguageName == newCulture.TwoLetterISOLanguageName)
            {
                return;
            }

            _currentCulture = newCulture;
            _preferencesService?.SaveLanguagePreference(newCulture.TwoLetterISOLanguageName);

            // Notify all properties changed to refresh UI
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public List<string> GetAvailableLanguages()
        {
            return new List<string> { "en", "de", "pl", "es", "pt", "sv" };
        }

        private static CultureInfo? TryCreateCulture(string? cultureCode)
        {
            if (string.IsNullOrWhiteSpace(cultureCode))
            {
                return null;
            }

            try
            {
                return new CultureInfo(cultureCode);
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }

        private static string? LoadLegacySavedLanguage()
        {
            string localPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TibiaHuntMaster",
                "preferences.json");

            string roamingPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TibiaHuntMaster",
                "preferences.json");

            return TryReadLegacyLanguage(localPath) ?? TryReadLegacyLanguage(roamingPath);
        }

        private static string? TryReadLegacyLanguage(string file)
        {
            try
            {
                if (!File.Exists(file))
                {
                    return null;
                }

                string json = File.ReadAllText(file);
                Dictionary<string, JsonElement>? preferences = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);

                if (preferences != null && preferences.TryGetValue("Language", out JsonElement languageElement))
                {
                    return languageElement.GetString();
                }
            }
            catch
            {
                // Ignore errors, will use default
            }

            return null;
        }
    }
}
