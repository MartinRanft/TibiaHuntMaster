using System.IO.Compression;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.App.Services.Diagnostics;

namespace TibiaHuntMaster.Tests.Services
{
    public sealed class DiagnosticsServiceTests
    {
        [Fact]
        public void RollingFileLoggerProvider_ShouldWriteLogEntryToDailyFile()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"thm-diagnostics-log-{Guid.NewGuid():N}");

            try
            {
                AppDataPaths paths = new(tempDirectory);
                using RollingFileLoggerProvider provider = new(paths);
                using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
                ILogger logger = loggerFactory.CreateLogger("DiagnosticsTest");

                logger.LogError("This is a test log entry.");

                string[] logFiles = Directory.GetFiles(paths.LogsDirectory, "app-*.log");
                logFiles.Should().ContainSingle();
                File.ReadAllText(logFiles[0]).Should().Contain("This is a test log entry.");
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        [Fact]
        public void CaptureExceptionReport_ShouldWriteCrashReport()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"thm-diagnostics-crash-{Guid.NewGuid():N}");

            try
            {
                AppDataPaths paths = new(tempDirectory);
                DiagnosticsService service = new(paths, NullLogger<DiagnosticsService>.Instance);

                service.CaptureExceptionReport(new InvalidOperationException("boom"), "UnitTest", isTerminating: false);

                string[] crashFiles = Directory.GetFiles(paths.CrashReportsDirectory, "crash-*.txt");
                crashFiles.Should().ContainSingle();
                string content = File.ReadAllText(crashFiles[0]);
                content.Should().Contain("UnitTest");
                content.Should().Contain("boom");
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        [Fact]
        public void CaptureExceptionReport_ShouldNotCreateArchive_WhenCrashIsNotTerminating()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"thm-diagnostics-nonfatal-{Guid.NewGuid():N}");

            try
            {
                AppDataPaths paths = new(tempDirectory);
                DiagnosticsService service = new(paths, NullLogger<DiagnosticsService>.Instance);

                service.CaptureExceptionReport(new InvalidOperationException("non fatal"), "UnitTest", isTerminating: false);

                Directory.GetFiles(paths.DiagnosticsExportsDirectory, "*.zip").Should().BeEmpty();
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        [Fact]
        public void CaptureExceptionReport_ShouldCreateArchive_WhenCrashIsTerminating()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"thm-diagnostics-fatal-{Guid.NewGuid():N}");

            try
            {
                AppDataPaths paths = new(tempDirectory);
                paths.EnsureDirectories();
                File.WriteAllText(Path.Combine(paths.LogsDirectory, "app-20260326.log"), "fatal log");
                DiagnosticsService service = new(paths, NullLogger<DiagnosticsService>.Instance);

                service.CaptureExceptionReport(new InvalidOperationException("fatal"), "UnitTest", isTerminating: true);

                string[] archives = Directory.GetFiles(paths.DiagnosticsExportsDirectory, "*.zip");
                archives.Should().ContainSingle();
                Path.GetFileName(archives[0]).Should().Contain("fatal-crash");
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        [Fact]
        public async Task ExportDiagnosticsAsync_ShouldCreateArchiveWithMetadataAndCollectedFiles()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"thm-diagnostics-export-{Guid.NewGuid():N}");

            try
            {
                AppDataPaths paths = new(tempDirectory);
                paths.EnsureDirectories();
                File.WriteAllText(Path.Combine(paths.LogsDirectory, "app-20260326.log"), "log content");
                File.WriteAllText(Path.Combine(paths.CrashReportsDirectory, "crash-20260326-123000.txt"), "crash content");

                DiagnosticsService service = new(paths, NullLogger<DiagnosticsService>.Instance);

                DiagnosticsExportResult result = await service.ExportDiagnosticsAsync();

                File.Exists(result.ArchivePath).Should().BeTrue();
                result.LogFilesIncluded.Should().Be(1);
                result.CrashFilesIncluded.Should().Be(1);

                using ZipArchive archive = ZipFile.OpenRead(result.ArchivePath);
                archive.Entries.Select(entry => entry.FullName).Should().Contain(new[]
                {
                    "README.txt",
                    "metadata.json",
                    "logs/app-20260326.log",
                    "crashes/crash-20260326-123000.txt"
                });
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }
    }
}
