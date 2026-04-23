#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Api
{
    public sealed class ApiDeltaFeedResponse
    {
        [JsonPropertyName("schemaVersion")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int SchemaVersion { get; set; }

        [JsonPropertyName("dataVersion")]
        public string DataVersion { get; set; } = string.Empty;

        [JsonPropertyName("generatedAtUtc")]
        public DateTimeOffset GeneratedAtUtc { get; set; }

        [JsonPropertyName("sinceUtc")]
        public DateTimeOffset SinceUtc { get; set; }

        [JsonPropertyName("latestChangeUtc")]
        public DateTimeOffset? LatestChangeUtc { get; set; }

        [JsonPropertyName("returnedCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int ReturnedCount { get; set; }

        [JsonPropertyName("hasMore")]
        public bool HasMore { get; set; }

        [JsonPropertyName("changes")]
        public List<ApiDeltaEntryResponse> Changes { get; set; } = [];
    }
}
