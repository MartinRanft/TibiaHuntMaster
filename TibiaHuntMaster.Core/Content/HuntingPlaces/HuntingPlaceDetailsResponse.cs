#nullable enable
using System.Text.Json.Serialization;

using TibiaHuntMaster.Core.Content.Shared;

namespace TibiaHuntMaster.Core.Content.HuntingPlaces
{
    public sealed class HuntingPlaceDetailsResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("plainTextContent")]
        public string? PlainTextContent { get; set; }

        [JsonPropertyName("rawWikiText")]
        public string? RawWikiText { get; set; }

        [JsonPropertyName("structuredData")]
        public HuntingPlaceStructuredDataResponse? StructuredData { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("vocation")]
        public string? Vocation { get; set; }

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

        [JsonPropertyName("loot")]
        public string? Loot { get; set; }

        [JsonPropertyName("lootStar")]
        public string? LootStar { get; set; }

        [JsonPropertyName("experience")]
        public string? Experience { get; set; }

        [JsonPropertyName("experienceStar")]
        public string? ExperienceStar { get; set; }

        [JsonPropertyName("bestLoot")]
        public string? BestLoot { get; set; }

        [JsonPropertyName("bestLoot2")]
        public string? BestLoot2 { get; set; }

        [JsonPropertyName("bestLoot3")]
        public string? BestLoot3 { get; set; }

        [JsonPropertyName("bestLoot4")]
        public string? BestLoot4 { get; set; }

        [JsonPropertyName("bestLoot5")]
        public string? BestLoot5 { get; set; }

        [JsonPropertyName("map")]
        public string? Map { get; set; }

        [JsonPropertyName("map2")]
        public string? Map2 { get; set; }

        [JsonPropertyName("map3")]
        public string? Map3 { get; set; }

        [JsonPropertyName("map4")]
        public string? Map4 { get; set; }

        [JsonPropertyName("creatures")]
        public List<HuntingPlaceCreatureResponse> Creatures { get; set; } = [];

        [JsonPropertyName("lowerLevels")]
        public List<HuntingPlaceAreaRecommendationResponse> LowerLevels { get; set; } = [];

        [JsonPropertyName("categories")]
        public List<WikiArticleCategoryResponse> Categories { get; set; } = [];

        [JsonPropertyName("wikiUrl")]
        public string? WikiUrl { get; set; }

        [JsonPropertyName("lastSeenAt")]
        public DateTimeOffset? LastSeenAt { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
