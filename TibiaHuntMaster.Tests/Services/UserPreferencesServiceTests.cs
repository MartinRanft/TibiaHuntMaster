using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TibiaHuntMaster.App.Services;

namespace TibiaHuntMaster.Tests.Services
{
    public sealed class UserPreferencesServiceTests
    {
        [Fact]
        public void Constructor_ShouldMigrateLegacyPreferences_WhenCurrentFileMissing()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "thm-prefs-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string currentPath = Path.Combine(tempDir, "current.json");
            string legacyPath = Path.Combine(tempDir, "legacy.json");
            File.WriteAllText(legacyPath, """{"Theme":"Dark","Language":"de"}""");

            try
            {
                UserPreferencesService service = new(
                    NullLogger<UserPreferencesService>.Instance,
                    currentPath,
                    legacyPath);

                File.Exists(currentPath).Should().BeTrue();
                service.GetLanguagePreference().Should().Be("de");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        [Fact]
        public void Constructor_ShouldNotOverwriteCurrentPreferences_WhenLegacyExists()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "thm-prefs-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string currentPath = Path.Combine(tempDir, "current.json");
            string legacyPath = Path.Combine(tempDir, "legacy.json");
            File.WriteAllText(currentPath, """{"Theme":"Light","Language":"en"}""");
            File.WriteAllText(legacyPath, """{"Theme":"Dark","Language":"de"}""");

            try
            {
                UserPreferencesService service = new(
                    NullLogger<UserPreferencesService>.Instance,
                    currentPath,
                    legacyPath);

                service.GetLanguagePreference().Should().Be("en");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        [Fact]
        public void SaveThemePreference_ShouldWriteAtomically_AndKeepBackupOfPreviousFile()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "thm-prefs-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string currentPath = Path.Combine(tempDir, "preferences.json");
            string backupPath = currentPath + ".bak";
            File.WriteAllText(currentPath, """{"Theme":"Dark","Language":"en"}""");

            try
            {
                UserPreferencesService service = new(
                    NullLogger<UserPreferencesService>.Instance,
                    currentPath,
                    legacyPreferencesFilePath: null);

                service.SaveThemePreference(AppTheme.Light);

                File.Exists(currentPath).Should().BeTrue();
                File.Exists(backupPath).Should().BeTrue();

                string currentJson = File.ReadAllText(currentPath);
                string backupJson = File.ReadAllText(backupPath);

                currentJson.Should().Contain("\"Theme\": \"Light\"");
                backupJson.Should().Contain("\"Theme\":\"Dark\"");
                Directory.GetFiles(tempDir, "*.tmp").Should().BeEmpty();
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        [Fact]
        public void Constructor_ShouldLoadBackupPreferences_WhenPrimaryFileIsCorrupted()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "thm-prefs-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string currentPath = Path.Combine(tempDir, "preferences.json");
            string backupPath = currentPath + ".bak";
            File.WriteAllText(currentPath, "{ this is not valid json");
            File.WriteAllText(backupPath, """{"Theme":"Light","Language":"sv","MinimapShowMarkers":false,"MinimapShowSpawns":true}""");

            try
            {
                UserPreferencesService service = new(
                    NullLogger<UserPreferencesService>.Instance,
                    currentPath,
                    legacyPreferencesFilePath: null);

                service.GetThemePreference().Should().Be(AppTheme.Light);
                service.GetLanguagePreference().Should().Be("sv");
                service.GetMinimapVisibilityPreferences().Should().Be((false, true));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        [Fact]
        public void Constructor_ShouldUseDefaults_WhenPrimaryAndBackupReadsFail()
        {
            string currentPath = "/virtual/preferences.json";
            string backupPath = currentPath + ".bak";

            FakePreferencesFileSystem fileSystem = new();
            fileSystem.ExistingFiles.Add(currentPath);
            fileSystem.ExistingFiles.Add(backupPath);
            fileSystem.ReadFailures.Add(currentPath, new IOException("primary read failed"));
            fileSystem.ReadFailures.Add(backupPath, new IOException("backup read failed"));

            UserPreferencesService service = new(
                NullLogger<UserPreferencesService>.Instance,
                currentPath,
                legacyPreferencesFilePath: null,
                fileSystem);

            service.GetThemePreference().Should().Be(AppTheme.Dark);
            service.GetLanguagePreference().Should().BeNull();
            service.GetMinimapVisibilityPreferences().Should().Be((true, true));
        }

        [Fact]
        public void SaveThemePreference_ShouldNotThrow_AndShouldCleanUpTempFile_WhenReplaceFails()
        {
            string currentPath = "/virtual/preferences.json";

            FakePreferencesFileSystem fileSystem = new();
            fileSystem.ExistingFiles.Add(currentPath);
            fileSystem.Contents[currentPath] = """{"Theme":"Dark","Language":"en"}""";
            fileSystem.ReplaceFailure = new IOException("replace failed");

            UserPreferencesService service = new(
                NullLogger<UserPreferencesService>.Instance,
                currentPath,
                legacyPreferencesFilePath: null,
                fileSystem);

            Action act = () => service.SaveThemePreference(AppTheme.Light);

            act.Should().NotThrow();
            service.GetThemePreference().Should().Be(AppTheme.Light);
            fileSystem.DeletedPaths.Should().ContainSingle(path => path.EndsWith(".tmp", StringComparison.Ordinal));
            fileSystem.ExistingFiles.Should().Contain(currentPath);
            fileSystem.Contents[currentPath].Should().Contain("\"Theme\":\"Dark\"");
        }

        private sealed class FakePreferencesFileSystem : UserPreferencesService.IUserPreferencesFileSystem
        {
            public Dictionary<string, string> Contents { get; } = new(StringComparer.Ordinal);
            public HashSet<string> ExistingFiles { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, Exception> ReadFailures { get; } = new(StringComparer.Ordinal);
            public List<string> DeletedPaths { get; } = [];
            public Exception? ReplaceFailure { get; set; }

            public bool FileExists(string path) => ExistingFiles.Contains(path);

            public void CopyFile(string sourcePath, string destinationPath)
            {
                string content = Contents[sourcePath];
                Contents[destinationPath] = content;
                ExistingFiles.Add(destinationPath);
            }

            public string ReadAllText(string path)
            {
                if (ReadFailures.TryGetValue(path, out Exception? exception))
                {
                    throw exception;
                }

                return Contents[path];
            }

            public void WriteAllText(string path, string contents)
            {
                Contents[path] = contents;
                ExistingFiles.Add(path);
            }

            public void ReplaceFile(string sourceFileName, string destinationFileName, string destinationBackupFileName)
            {
                if (ReplaceFailure != null)
                {
                    throw ReplaceFailure;
                }

                if (Contents.TryGetValue(destinationFileName, out string? previous))
                {
                    Contents[destinationBackupFileName] = previous;
                    ExistingFiles.Add(destinationBackupFileName);
                }

                Contents[destinationFileName] = Contents[sourceFileName];
                ExistingFiles.Add(destinationFileName);
                Contents.Remove(sourceFileName);
                ExistingFiles.Remove(sourceFileName);
            }

            public void MoveFile(string sourceFileName, string destinationFileName)
            {
                Contents[destinationFileName] = Contents[sourceFileName];
                ExistingFiles.Add(destinationFileName);
                Contents.Remove(sourceFileName);
                ExistingFiles.Remove(sourceFileName);
            }

            public void DeleteFile(string path)
            {
                DeletedPaths.Add(path);
                Contents.Remove(path);
                ExistingFiles.Remove(path);
            }

            public void CreateDirectory(string path)
            {
            }
        }
    }
}
