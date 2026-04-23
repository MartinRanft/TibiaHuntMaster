#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bosstiary
{
    public sealed class BosstiaryCategoryCreaturesResponse
    {
        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("categorySlug")]
        public string CategorySlug { get; set; } = string.Empty;

        [JsonPropertyName("sortOrder")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int SortOrder { get; set; }

        [JsonPropertyName("totalPoints")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int TotalPoints { get; set; }

        [JsonPropertyName("totalKillsRequired")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int TotalKillsRequired { get; set; }

        [JsonPropertyName("levelRequirements")]
        public List<BosstiaryLevelRequirementResponse> LevelRequirements { get; set; } = [];

        [JsonPropertyName("creatureCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CreatureCount { get; set; }

        [JsonPropertyName("creatures")]
        public List<BosstiaryCreatureListItemResponse> Creatures { get; set; } = [];
    }
}
