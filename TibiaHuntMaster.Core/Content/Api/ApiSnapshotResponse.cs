#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Api
{
    public sealed class ApiSnapshotResponse
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

        [JsonPropertyName("resources")]
        public List<ApiSnapshotResourceResponse> Resources { get; set; } = [];
    }
}
