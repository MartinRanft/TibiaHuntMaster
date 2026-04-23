#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bestiary
{
    public sealed class BestiaryClassResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("sortOrder")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int SortOrder { get; set; }

        [JsonPropertyName("categoryCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CategoryCount { get; set; }

        [JsonPropertyName("creatureCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CreatureCount { get; set; }
    }
}
