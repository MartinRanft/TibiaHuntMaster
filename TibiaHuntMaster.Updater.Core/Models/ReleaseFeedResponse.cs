using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Updater.Core.Models
{
    public sealed class ReleaseFeedResponse
    {
        [JsonPropertyName("version")]
        public required string Version { get; init; }

        [JsonPropertyName("tag")]
        public required string Tag { get; init; }

        [JsonPropertyName("channel")]
        public required string Channel { get; init; }

        [JsonPropertyName("publishedAtUtc")]
        public required DateTimeOffset PublishedAtUtc { get; init; }

        [JsonPropertyName("windowsX64")]
        public ReleaseFeedAssetResponse? WindowsX64 { get; init; }

        [JsonPropertyName("linuxX64")]
        public ReleaseFeedAssetResponse? LinuxX64 { get; init; }

        [JsonPropertyName("osxX64")]
        public ReleaseFeedAssetResponse? OsxX64 { get; init; }

        [JsonPropertyName("osxArm64")]
        public ReleaseFeedAssetResponse? OsxArm64 { get; init; }
    }
}
