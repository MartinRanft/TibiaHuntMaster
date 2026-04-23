#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bestiary
{
    public sealed class BestiaryCharmPointOverviewItemResponse
    {
        [JsonPropertyName("creatureId")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CreatureId { get; set; }

        [JsonPropertyName("creatureName")]
        public string CreatureName { get; set; } = string.Empty;

        [JsonPropertyName("className")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = string.Empty;

        [JsonPropertyName("difficultySortOrder")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int DifficultySortOrder { get; set; }

        [JsonPropertyName("charmPoints")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CharmPoints { get; set; }

        [JsonPropertyName("totalKillsRequired")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int TotalKillsRequired { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
