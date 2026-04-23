#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Api
{
    public sealed class ApiSnapshotResourceResponse
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Count { get; set; }

        [JsonPropertyName("latestUpdateUtc")]
        public DateTimeOffset? LatestUpdateUtc { get; set; }

        [JsonPropertyName("listRoute")]
        public string? ListRoute { get; set; }

        [JsonPropertyName("detailByNameRoutePattern")]
        public string? DetailByNameRoutePattern { get; set; }

        [JsonPropertyName("detailByIdRoutePattern")]
        public string? DetailByIdRoutePattern { get; set; }

        [JsonPropertyName("syncRoute")]
        public string? SyncRoute { get; set; }

        [JsonPropertyName("syncByDateRoute")]
        public string? SyncByDateRoute { get; set; }

        [JsonPropertyName("relatedRoutes")]
        public List<string> RelatedRoutes { get; set; } = [];
    }
}
