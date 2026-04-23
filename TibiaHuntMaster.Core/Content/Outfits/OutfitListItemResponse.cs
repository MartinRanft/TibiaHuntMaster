#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Outfits
{
    public sealed class OutfitListItemResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("wikiUrl")]
        public string? WikiUrl { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
