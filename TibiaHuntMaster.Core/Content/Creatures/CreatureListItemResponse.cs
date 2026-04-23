#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Creatures
{
    public sealed class CreatureListItemResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("hitpoints")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Hitpoints { get; set; }

        [JsonPropertyName("experience")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Experience { get; set; }

        [JsonPropertyName("primaryImage")]
        public CreatureImageResponse? PrimaryImage { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
