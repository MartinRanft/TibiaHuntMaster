namespace TibiaHuntMaster.Updater.Core.Models
{
    public sealed class UpdatePlan
    {
        public required string CurrentVersion { get; init; }

        public required string TargetVersion { get; init; }

        public required string Tag { get; init; }

        public required string Channel { get; init; }

        public required DateTimeOffset PublishedAtUtc { get; init; }

        public ReleaseFeedAssetResponse? Asset { get; init; }

        public string? ReleasePageUrl { get; init; }
    }
}
