#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bestiary
{
    public sealed class BestiaryDifficultyCreaturesResponse
    {
        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = string.Empty;

        [JsonPropertyName("difficultySlug")]
        public string DifficultySlug { get; set; } = string.Empty;

        [JsonPropertyName("sortOrder")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int SortOrder { get; set; }

        [JsonPropertyName("charmPoints")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CharmPoints { get; set; }

        [JsonPropertyName("totalKillsRequired")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int TotalKillsRequired { get; set; }

        [JsonPropertyName("levelRequirements")]
        public List<BestiaryLevelRequirementResponse> LevelRequirements { get; set; } = [];

        [JsonPropertyName("creatureCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CreatureCount { get; set; }

        [JsonPropertyName("creatures")]
        public List<BestiaryCreatureListItemResponse> Creatures { get; set; } = [];
    }
}
