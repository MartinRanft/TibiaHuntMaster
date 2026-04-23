using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace TibiaHuntMaster.App.Services.Diagnostics
{
    public sealed class DiagnosticsService : IDiagnosticsService
    {
        private const int MaxCrashReports = 20;
        private const int MaxExportedLogFiles = 5;
        private const int MaxExportedCrashFiles = 10;
        private readonly object _exportLock = new();

        private readonly AppDataPaths _paths;
        private readonly UserPreferencesService? _preferencesService;
        private readonly ILogger<DiagnosticsService> _logger;

        public DiagnosticsService(
            AppDataPaths paths,
            ILogger<DiagnosticsService> logger,
            UserPreferencesService? preferencesService = null)
        {
            _paths = paths;
            _logger = logger;
            _preferencesService = preferencesService;
            _paths.EnsureDirectories();
        }

        public string LogsDirectory => _paths.LogsDirectory;

        public string DiagnosticsExportsDirectory => _paths.DiagnosticsExportsDirectory;

        public void CaptureExceptionReport(Exception exception, string source, bool isTerminating = false)
        {
            ArgumentNullException.ThrowIfNull(exception);

            try
            {
                _paths.EnsureDirectories();
                TrimOldCrashReports();

                string fileName = $"crash-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.txt";
                string filePath = Path.Combine(_paths.CrashReportsDirectory, fileName);

                StringBuilder builder = new();
                builder.AppendLine("TibiaHuntMaster crash report");
                builder.AppendLine($"TimestampUtc: {DateTimeOffset.UtcNow:O}");
                builder.AppendLine($"Source: {source}");
                builder.AppendLine($"IsTerminating: {isTerminating}");
                builder.AppendLine($"OS: {RuntimeInformation.OSDescription}");
                builder.AppendLine($"Framework: {RuntimeInformation.FrameworkDescription}");
                builder.AppendLine($"ProcessArchitecture: {RuntimeInformation.ProcessArchitecture}");
                builder.AppendLine();
                builder.AppendLine(exception.ToString());

                File.WriteAllText(filePath, builder.ToString(), Encoding.UTF8);

                if (isTerminating)
                {
                    CreateDiagnosticsArchive(exportReason: "fatal-crash");
                }
            }
            catch
            {
                // Crash capture must not throw while processing an already failing path.
            }
        }

        public async Task<DiagnosticsExportResult> ExportDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(
                () => CreateDiagnosticsArchive(exportReason: "manual-export", cancellationToken),
                cancellationToken);
        }

        private object BuildMetadata(int logFilesIncluded, int crashFilesIncluded)
        {
            FileInfo? databaseInfo = File.Exists(_paths.DatabasePath)
                ? new FileInfo(_paths.DatabasePath)
                : null;

            UserPreferences? preferences = _preferencesService?.Preferences;
            Assembly entryAssembly = Assembly.GetEntryAssembly() ?? typeof(DiagnosticsService).Assembly;
            string version = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                             ?? entryAssembly.GetName().Version?.ToString()
                             ?? "unknown";

            return new
            {
                exportedAtUtc = DateTimeOffset.UtcNow,
                appVersion = version,
                osDescription = RuntimeInformation.OSDescription,
                osArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                framework = RuntimeInformation.FrameworkDescription,
                culture = CultureInfo.CurrentCulture.Name,
                uiCulture = CultureInfo.CurrentUICulture.Name,
                diagnostics = new
                {
                    logFilesIncluded,
                    crashFilesIncluded
                },
                database = new
                {
                    exists = databaseInfo != null,
                    sizeBytes = databaseInfo?.Length,
                    lastWriteUtc = databaseInfo?.LastWriteTimeUtc
                },
                preferences = preferences == null
                    ? null
                    : new
                    {
                        theme = preferences.Theme,
                        language = preferences.Language,
                        minimapShowMarkers = preferences.MinimapShowMarkers,
                        minimapShowSpawns = preferences.MinimapShowSpawns
                    }
            };
        }

        private static string BuildReadmeText()
        {
            return string.Join(Environment.NewLine,
            [
                "TibiaHuntMaster diagnostics export",
                string.Empty,
                "Attach this archive to your GitHub issue.",
                string.Empty,
                "Please also describe:",
                "- what you were doing",
                "- what you expected to happen",
                "- what actually happened",
                "- when it happened",
                string.Empty,
                "This archive intentionally excludes your SQLite database and raw clipboard content."
            ]);
        }

        private static void AddTextEntry(ZipArchive archive, string entryName, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using StreamWriter writer = new(entry.Open(), Encoding.UTF8);
            writer.Write(content);
        }

        private DiagnosticsExportResult CreateDiagnosticsArchive(string exportReason, CancellationToken cancellationToken = default)
        {
            lock (_exportLock)
            {
                _paths.EnsureDirectories();

                List<string> logFiles = Directory.Exists(_paths.LogsDirectory)
                    ? Directory.EnumerateFiles(_paths.LogsDirectory, "app-*.log")
                               .OrderByDescending(File.GetLastWriteTimeUtc)
                               .Take(MaxExportedLogFiles)
                               .ToList()
                    : [];

                List<string> crashFiles = Directory.Exists(_paths.CrashReportsDirectory)
                    ? Directory.EnumerateFiles(_paths.CrashReportsDirectory, "crash-*.txt")
                               .OrderByDescending(File.GetLastWriteTimeUtc)
                               .Take(MaxExportedCrashFiles)
                               .ToList()
                    : [];

                string archivePath = Path.Combine(
                    _paths.DiagnosticsExportsDirectory,
                    $"tibiahuntmaster-diagnostics-{exportReason}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");

                using FileStream stream = new(archivePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                using ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: false);

                foreach (string logFile in logFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    archive.CreateEntryFromFile(logFile, $"logs/{Path.GetFileName(logFile)}");
                }

                foreach (string crashFile in crashFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    archive.CreateEntryFromFile(crashFile, $"crashes/{Path.GetFileName(crashFile)}");
                }

                AddTextEntry(
                    archive,
                    "README.txt",
                    BuildReadmeText());

                AddTextEntry(
                    archive,
                    "metadata.json",
                    JsonSerializer.Serialize(BuildMetadata(logFiles.Count, crashFiles.Count), new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));

                _logger.LogInformation(
                    "Exported diagnostics archive to {ArchivePath} with {LogFiles} log files and {CrashFiles} crash files ({ExportReason}).",
                    archivePath,
                    logFiles.Count,
                    crashFiles.Count,
                    exportReason);

                return new DiagnosticsExportResult(archivePath, logFiles.Count, crashFiles.Count);
            }
        }

        private void TrimOldCrashReports()
        {
            IEnumerable<string> staleFiles = Directory.EnumerateFiles(_paths.CrashReportsDirectory, "crash-*.txt")
                                                    .OrderByDescending(File.GetLastWriteTimeUtc)
                                                    .Skip(MaxCrashReports);

            foreach (string staleFile in staleFiles)
            {
                try
                {
                    File.Delete(staleFile);
                }
                catch
                {
                    // Best effort only.
                }
            }
        }
    }
}
