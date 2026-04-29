using NuGet.Versioning;
using IVersionComparer = TibiaHuntMaster.Updater.Core.Abstractions.IVersionComparer;

namespace TibiaHuntMaster.Updater.Core.Services.Versioning
{
    public sealed class VersionComparer : IVersionComparer
    {
        public int Compare(string currentVersion, string remoteVersion)
        {
            if (!NuGetVersion.TryParse(currentVersion, out NuGetVersion? current))
                throw new ArgumentException($"Invalid version string: {currentVersion}", nameof(currentVersion));

            if (!NuGetVersion.TryParse(remoteVersion, out NuGetVersion? remote))
                throw new ArgumentException($"Invalid version string: {remoteVersion}", nameof(remoteVersion));

            return remote.CompareTo(current);
        }
    }
}