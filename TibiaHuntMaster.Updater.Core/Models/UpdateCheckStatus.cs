namespace TibiaHuntMaster.Updater.Core.Models
{
    public enum UpdateCheckStatus
    {
        UpToDate = 0,
        UpdateAvailable = 1,
        FeedUnavailable = 2,
        InvalidFeed = 3,
        UnsupportedPlatform = 4,
        Failed = 5,
    }
}
