#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Api
{
    public sealed class ApiDeltaEntryResponse
    {
        [JsonPropertyName("resource")]
        public string Resource { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("identifier")]
        public string Identifier { get; set; } = string.Empty;

        [JsonPropertyName("updatedAtUtc")]
        public DateTimeOffset UpdatedAtUtc { get; set; }

        [JsonPropertyName("changeType")]
        public string ChangeType { get; set; } = string.Empty;

        [JsonPropertyName("route")]
        public string Route { get; set; } = string.Empty;

        [JsonPropertyName("relatedRoutes")]
        public List<string> RelatedRoutes { get; set; } = [];
    }
}
