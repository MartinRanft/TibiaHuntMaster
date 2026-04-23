using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TibiaHuntMaster.App.Services.Diagnostics
{
    public sealed class FileRevealService : IFileRevealService
    {
        public Task RevealFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            ProcessStartInfo startInfo = BuildStartInfo(filePath);
            Process.Start(startInfo);
            return Task.CompletedTask;
        }

        internal static ProcessStartInfo BuildStartInfo(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);
            string directoryPath = Path.GetDirectoryName(fullPath)
                                   ?? throw new InvalidOperationException("Could not determine parent directory for diagnostics export.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{fullPath}\"",
                    UseShellExecute = true
                };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-R \"{fullPath}\"",
                    UseShellExecute = false
                };
            }

            return new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{directoryPath}\"",
                UseShellExecute = false
            };
        }
    }
}
