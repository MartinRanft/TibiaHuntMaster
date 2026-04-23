#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Charms
{
    public sealed class CharmListItemResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("actualName")]
        public string? ActualName { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("cost")]
        public string Cost { get; set; } = string.Empty;

        [JsonPropertyName("effect")]
        public string Effect { get; set; } = string.Empty;

        [JsonPropertyName("wikiUrl")]
        public string? WikiUrl { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
