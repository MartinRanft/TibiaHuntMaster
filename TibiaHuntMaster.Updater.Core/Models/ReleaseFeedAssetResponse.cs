using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Updater.Core.Models
{
    public sealed class ReleaseFeedAssetResponse
    {
        [JsonPropertyName("fileName")]
        public required string FileName { get; init; }

        [JsonPropertyName("url")]
        public required string Url { get; init; }

        [JsonPropertyName("sha256")]
        public required string Sha256 { get; init; }
    }
}
