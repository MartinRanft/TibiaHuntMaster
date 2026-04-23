#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.HuntingPlaces
{
    public sealed class HuntingPlaceAreaRecommendationResponse
    {
        [JsonPropertyName("areaName")]
        public string? AreaName { get; set; }

        [JsonPropertyName("levelKnights")]
        public string? LevelKnights { get; set; }

        [JsonPropertyName("levelPaladins")]
        public string? LevelPaladins { get; set; }

        [JsonPropertyName("levelMages")]
        public string? LevelMages { get; set; }

        [JsonPropertyName("skillKnights")]
        public string? SkillKnights { get; set; }

        [JsonPropertyName("skillPaladins")]
        public string? SkillPaladins { get; set; }

        [JsonPropertyName("skillMages")]
        public string? SkillMages { get; set; }

        [JsonPropertyName("defenseKnights")]
        public string? DefenseKnights { get; set; }

        [JsonPropertyName("defensePaladins")]
        public string? DefensePaladins { get; set; }

        [JsonPropertyName("defenseMages")]
        public string? DefenseMages { get; set; }
    }
}
