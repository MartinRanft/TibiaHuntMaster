#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Api
{
    public sealed class ApiVersionResponse
    {
        [JsonPropertyName("schemaVersion")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int SchemaVersion { get; set; }

        [JsonPropertyName("dataVersion")]
        public string DataVersion { get; set; } = string.Empty;

        [JsonPropertyName("generatedAtUtc")]
        public DateTimeOffset GeneratedAtUtc { get; set; }

        [JsonPropertyName("latestDataUpdateUtc")]
        public DateTimeOffset? LatestDataUpdateUtc { get; set; }

        [JsonPropertyName("itemCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int ItemCount { get; set; }

        [JsonPropertyName("wikiArticleCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int WikiArticleCount { get; set; }

        [JsonPropertyName("creatureCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CreatureCount { get; set; }

        [JsonPropertyName("categoryCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CategoryCount { get; set; }

        [JsonPropertyName("assetCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int AssetCount { get; set; }
    }
}
