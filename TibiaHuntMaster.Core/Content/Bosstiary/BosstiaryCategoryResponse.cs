#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bosstiary
{
    public sealed class BosstiaryCategoryResponse
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

        [JsonPropertyName("totalPoints")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int TotalPoints { get; set; }

        [JsonPropertyName("totalKillsRequired")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int TotalKillsRequired { get; set; }

        [JsonPropertyName("creatureCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CreatureCount { get; set; }
    }
}
