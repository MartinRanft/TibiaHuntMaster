#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bosstiary
{
    public sealed class BosstiaryCreatureListItemResponse
    {
        [JsonPropertyName("creatureId")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CreatureId { get; set; }

        [JsonPropertyName("creatureName")]
        public string CreatureName { get; set; } = string.Empty;

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("categorySlug")]
        public string CategorySlug { get; set; } = string.Empty;

        [JsonPropertyName("categorySortOrder")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CategorySortOrder { get; set; }

        [JsonPropertyName("totalPoints")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int TotalPoints { get; set; }

        [JsonPropertyName("totalKillsRequired")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int TotalKillsRequired { get; set; }

        [JsonPropertyName("levelRequirements")]
        public List<BosstiaryLevelRequirementResponse> LevelRequirements { get; set; } = [];

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
