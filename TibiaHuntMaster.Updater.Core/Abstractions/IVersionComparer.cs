namespace TibiaHuntMaster.Updater.Core.Abstractions
{
    public interface IVersionComparer
    {
        int Compare(string leftVersion, string rightVersion);
    }
}
