using System;
using System.IO;

namespace TibiaHuntMaster.App.Services.Diagnostics
{
    public sealed class AppDataPaths
    {
        public AppDataPaths(string? baseDirectory = null)
        {
            BaseDirectory = baseDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TibiaHuntMaster");

            DatabasePath = Path.Combine(BaseDirectory, "tibiahuntmaster.db");
            PreferencesFilePath = Path.Combine(BaseDirectory, "preferences.json");
            SummariesDirectory = Path.Combine(BaseDirectory, "Summaries");
            LogsDirectory = Path.Combine(BaseDirectory, "Logs");
            CrashReportsDirectory = Path.Combine(BaseDirectory, "Crashes");
            DiagnosticsDirectory = Path.Combine(BaseDirectory, "Diagnostics");
            DiagnosticsExportsDirectory = Path.Combine(DiagnosticsDirectory, "Exports");
        }

        public string BaseDirectory { get; }

        public string DatabasePath { get; }

        public string PreferencesFilePath { get; }

        public string SummariesDirectory { get; }

        public string LogsDirectory { get; }

        public string CrashReportsDirectory { get; }

        public string DiagnosticsDirectory { get; }

        public string DiagnosticsExportsDirectory { get; }

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(BaseDirectory);
            Directory.CreateDirectory(SummariesDirectory);
            Directory.CreateDirectory(LogsDirectory);
            Directory.CreateDirectory(CrashReportsDirectory);
            Directory.CreateDirectory(DiagnosticsExportsDirectory);
        }
    }
}
