using NuGet.Versioning;

namespace TibiaHuntMaster.Updater.Core.Constants
{
    internal static class VersionHelper
    {
        internal static bool IsRemoteVersionNewer(string currentVersion, string remoteVersion)
        {
            if (!NuGetVersion.TryParse(currentVersion, out NuGetVersion? current))
                throw new ArgumentException($"Unexpected actual Version: {currentVersion}");

            if (!NuGetVersion.TryParse(remoteVersion, out NuGetVersion? remote))
                throw new ArgumentException($"Unexpected Remote-Version: {remoteVersion}");

            return remote > current;
        }
    }
}