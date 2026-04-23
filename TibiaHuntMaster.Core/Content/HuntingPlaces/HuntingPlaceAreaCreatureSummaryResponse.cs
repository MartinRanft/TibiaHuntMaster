#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.HuntingPlaces
{
    public sealed class HuntingPlaceAreaCreatureSummaryResponse
    {
        [JsonPropertyName("areaName")]
        public string AreaName { get; set; } = string.Empty;

        [JsonPropertyName("sectionName")]
        public string? SectionName { get; set; }

        [JsonPropertyName("creatureCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CreatureCount { get; set; }

        [JsonPropertyName("creatures")]
        public List<HuntingPlaceCreatureResponse> Creatures { get; set; } = [];

        [JsonPropertyName("recommendedLevels")]
        public HuntingPlaceVocationValueResponse? RecommendedLevels { get; set; }

        [JsonPropertyName("recommendedSkills")]
        public HuntingPlaceVocationValueResponse? RecommendedSkills { get; set; }

        [JsonPropertyName("recommendedDefense")]
        public HuntingPlaceVocationValueResponse? RecommendedDefense { get; set; }
    }
}
