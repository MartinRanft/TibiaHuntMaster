using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TibiaHuntMaster.App.Services
{
    /// <summary>
    /// User preferences that are persisted to disk
    /// </summary>
    public sealed class UserPreferences
    {
        public string Theme { get; set; } = "Dark";
        public string? Language { get; set; }
        public bool MinimapShowMarkers { get; set; } = true;
        public bool MinimapShowSpawns { get; set; } = true;
        public int UpdateCheckFailureStartCount { get; set; }
        public string? DeferredUpdateVersion { get; set; }
        public int DeferredUpdateStartCount { get; set; }
    }

    /// <summary>
    /// Service for managing user preferences persistence
    /// </summary>
    public sealed class UserPreferencesService
    {
        internal interface IUserPreferencesFileSystem
        {
            bool FileExists(string path);
            void CopyFile(string sourcePath, string destinationPath);
            string ReadAllText(string path);
            void WriteAllText(string path, string contents);
            void ReplaceFile(string sourceFileName, string destinationFileName, string destinationBackupFileName);
            void MoveFile(string sourceFileName, string destinationFileName);
            void DeleteFile(string path);
            void CreateDirectory(string path);
        }

        internal sealed class UserPreferencesFileSystem : IUserPreferencesFileSystem
        {
            public bool FileExists(string path) => File.Exists(path);
            public void CopyFile(string sourcePath, string destinationPath) => File.Copy(sourcePath, destinationPath);
            public string ReadAllText(string path) => File.ReadAllText(path);
            public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
            public void ReplaceFile(string sourceFileName, string destinationFileName, string destinationBackupFileName) =>
            File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors: true);
            public void MoveFile(string sourceFileName, string destinationFileName) => File.Move(sourceFileName, destinationFileName);
            public void DeleteFile(string path) => File.Delete(path);
            public void CreateDirectory(string path) => Directory.CreateDirectory(path);
        }

        private readonly IUserPreferencesFileSystem _fileSystem;
        private readonly ILogger<UserPreferencesService> _logger;
        private readonly string? _legacyPreferencesFilePath;
        private readonly string _preferencesBackupFilePath;
        private readonly string _preferencesFilePath;
        private UserPreferences _preferences;

        public UserPreferencesService(
            ILogger<UserPreferencesService>? logger = null,
            string? preferencesFilePath = null,
            string? legacyPreferencesFilePath = null)
        : this(logger, preferencesFilePath, legacyPreferencesFilePath, new UserPreferencesFileSystem())
        {
        }

        internal UserPreferencesService(
            ILogger<UserPreferencesService>? logger,
            string? preferencesFilePath,
            string? legacyPreferencesFilePath,
            IUserPreferencesFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _logger = logger ?? NullLogger<UserPreferencesService>.Instance;
            _preferencesFilePath = preferencesFilePath ?? GetPreferencesFilePath();
            _preferencesBackupFilePath = GetBackupFilePath(_preferencesFilePath);
            _legacyPreferencesFilePath = legacyPreferencesFilePath ?? GetLegacyPreferencesFilePath();
            TryMigrateLegacyPreferences();
            _preferences = LoadPreferences();
        }

        /// <summary>
        /// Gets the current user preferences
        /// </summary>
        public UserPreferences Preferences => _preferences;

        /// <summary>
        /// Gets the theme preference
        /// </summary>
        public AppTheme GetThemePreference()
        {
            return Enum.TryParse<AppTheme>(_preferences.Theme, out AppTheme theme)
            ? theme
            : AppTheme.Dark;
        }

        /// <summary>
        /// Saves the theme preference
        /// </summary>
        public void SaveThemePreference(AppTheme theme)
        {
            _preferences.Theme = theme.ToString();
            SavePreferences();
        }

        /// <summary>
        /// Gets the saved language preference (if any).
        /// </summary>
        public string? GetLanguagePreference()
        {
            if (string.IsNullOrWhiteSpace(_preferences.Language))
            {
                return null;
            }

            return _preferences.Language;
        }

        /// <summary>
        /// Saves the language preference.
        /// </summary>
        public void SaveLanguagePreference(string cultureCode)
        {
            if (string.IsNullOrWhiteSpace(cultureCode))
            {
                return;
            }

            _preferences.Language = cultureCode;
            SavePreferences();
        }

        public (bool ShowMarkers, bool ShowSpawns) GetMinimapVisibilityPreferences()
        {
            return (_preferences.MinimapShowMarkers, _preferences.MinimapShowSpawns);
        }

        public void SaveMinimapVisibilityPreferences(bool showMarkers, bool showSpawns)
        {
            _preferences.MinimapShowMarkers = showMarkers;
            _preferences.MinimapShowSpawns = showSpawns;
            SavePreferences();
        }

        public int RegisterUpdateCheckFailure()
        {
            _preferences.UpdateCheckFailureStartCount++;
            SavePreferences();
            return _preferences.UpdateCheckFailureStartCount;
        }

        public void ResetUpdateCheckFailureCounter()
        {
            if(_preferences.UpdateCheckFailureStartCount == 0)
            {
                return;
            }

            _preferences.UpdateCheckFailureStartCount = 0;
            SavePreferences();
        }

        public bool ShouldShowDeferredUpdatePrompt(string targetVersion, int reminderIntervalStartCount)
        {
            if(string.IsNullOrWhiteSpace(targetVersion) || reminderIntervalStartCount <= 0)
            {
                return true;
            }

            if(!string.Equals(_preferences.DeferredUpdateVersion, targetVersion, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            _preferences.DeferredUpdateStartCount++;
            SavePreferences();

            return _preferences.DeferredUpdateStartCount >= reminderIntervalStartCount;
        }

        public void DeferUpdatePrompt(string targetVersion)
        {
            if(string.IsNullOrWhiteSpace(targetVersion))
            {
                return;
            }

            _preferences.DeferredUpdateVersion = targetVersion;
            _preferences.DeferredUpdateStartCount = 0;
            SavePreferences();
        }

        public void ClearDeferredUpdatePrompt()
        {
            if(string.IsNullOrWhiteSpace(_preferences.DeferredUpdateVersion) && _preferences.DeferredUpdateStartCount == 0)
            {
                return;
            }

            _preferences.DeferredUpdateVersion = null;
            _preferences.DeferredUpdateStartCount = 0;
            SavePreferences();
        }

        private string GetPreferencesFilePath()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
            string path = Path.Combine(basePath, "TibiaHuntMaster", "preferences.json");
        
            _fileSystem.CreateDirectory(Path.GetDirectoryName(path)!);
        
            return path;
        }

        private string GetLegacyPreferencesFilePath()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(basePath, "TibiaHuntMaster", "preferences.json");
        }

        private void TryMigrateLegacyPreferences()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_legacyPreferencesFilePath))
                {
                    return;
                }

                if (string.Equals(_legacyPreferencesFilePath, _preferencesFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (_fileSystem.FileExists(_preferencesFilePath) || !_fileSystem.FileExists(_legacyPreferencesFilePath))
                {
                    return;
                }

                _fileSystem.CreateDirectory(Path.GetDirectoryName(_preferencesFilePath)!);
                _fileSystem.CopyFile(_legacyPreferencesFilePath, _preferencesFilePath);
                _logger.LogInformation("Migrated legacy user preferences from {LegacyPath} to {CurrentPath}", _legacyPreferencesFilePath, _preferencesFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to migrate legacy preferences from {LegacyPath} to {CurrentPath}", _legacyPreferencesFilePath, _preferencesFilePath);
            }
        }

        private UserPreferences LoadPreferences()
        {
            UserPreferences? primary = TryLoadPreferencesFromPath(_preferencesFilePath, isBackup: false);
            if (primary != null)
            {
                return primary;
            }

            UserPreferences? backup = TryLoadPreferencesFromPath(_preferencesBackupFilePath, isBackup: true);
            if (backup != null)
            {
                return backup;
            }

            _logger.LogInformation("Using default user preferences");
            return new UserPreferences();
        }

        private void SavePreferences()
        {
            string? tempFilePath = null;

            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(_preferences, options);

                string directoryPath = Path.GetDirectoryName(_preferencesFilePath)
                                       ?? throw new InvalidOperationException("Preferences directory path is invalid.");
                _fileSystem.CreateDirectory(directoryPath);

                tempFilePath = Path.Combine(directoryPath, $"{Path.GetFileName(_preferencesFilePath)}.{Guid.NewGuid():N}.tmp");
                _fileSystem.WriteAllText(tempFilePath, json);

                if (_fileSystem.FileExists(_preferencesFilePath))
                {
                    _fileSystem.ReplaceFile(tempFilePath, _preferencesFilePath, _preferencesBackupFilePath);
                }
                else
                {
                    _fileSystem.MoveFile(tempFilePath, _preferencesFilePath);
                }

                _logger.LogInformation("Saved user preferences to {Path}", _preferencesFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save user preferences to {Path}", _preferencesFilePath);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempFilePath) && _fileSystem.FileExists(tempFilePath))
                {
                    try
                    {
                        _fileSystem.DeleteFile(tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to clean up temporary preferences file {Path}", tempFilePath);
                    }
                }
            }
        }

        private UserPreferences? TryLoadPreferencesFromPath(string path, bool isBackup)
        {
            try
            {
                if (!_fileSystem.FileExists(path))
                {
                    return null;
                }

                string json = _fileSystem.ReadAllText(path);
                UserPreferences? preferences = JsonSerializer.Deserialize<UserPreferences>(json);
                if (preferences != null)
                {
                    _logger.LogInformation(
                        "Loaded user preferences from {Path}{BackupSuffix}",
                        path,
                        isBackup ? " (backup)" : string.Empty);
                    return preferences;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load user preferences from {Path}{BackupSuffix}",
                    path,
                    isBackup ? " (backup)" : string.Empty);
            }

            return null;
        }

        private static string GetBackupFilePath(string preferencesFilePath)
        {
            return $"{preferencesFilePath}.bak";
        }
    }
}
