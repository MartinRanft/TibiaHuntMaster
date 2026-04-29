using System.IO.Compression;
using TibiaHuntMaster.Updater.Core.Abstractions;

namespace TibiaHuntMaster.Updater.Core.Services.Packages
{
    public sealed class ZipPackageExtractor : IUpdatePackageExtractor
    {
        public async Task<string> ExtractAsync(string archiveFilePath, string targetDirectory, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                if (Directory.Exists(targetDirectory))
                    Directory.Delete(targetDirectory, recursive: true);

                ZipFile.ExtractToDirectory(archiveFilePath, targetDirectory);
            }, cancellationToken);

            return targetDirectory;
        }
    }
}
