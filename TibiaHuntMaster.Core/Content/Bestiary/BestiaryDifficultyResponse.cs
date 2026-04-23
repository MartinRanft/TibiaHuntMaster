#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bestiary
{
    public sealed class BestiaryDifficultyResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

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
    }
}
