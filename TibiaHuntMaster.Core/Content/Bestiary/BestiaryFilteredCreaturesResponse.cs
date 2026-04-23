#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bestiary
{
    public sealed class BestiaryFilteredCreaturesResponse
    {
        [JsonPropertyName("bestiaryClass")]
        public string? BestiaryClass { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("difficulty")]
        public string? Difficulty { get; set; }

        [JsonPropertyName("charmPointReward")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? CharmPointReward { get; set; }

        [JsonPropertyName("creatureName")]
        public string? CreatureName { get; set; }

        [JsonPropertyName("sort")]
        public string? Sort { get; set; }

        [JsonPropertyName("descending")]
        public bool DescendingValue { get; set; }

        [JsonPropertyName("page")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int PageSize { get; set; }

        [JsonPropertyName("totalCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int TotalCount { get; set; }

        [JsonPropertyName("creatures")]
        public List<BestiaryCreatureListItemResponse> Creatures { get; set; } = [];
    }
}
