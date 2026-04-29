namespace TibiaHuntMaster.Updater.Core.Abstractions
{
    public interface IVersionComparer
    {
        int Compare(string currentVersion, string remoteVersion);
    }
}
