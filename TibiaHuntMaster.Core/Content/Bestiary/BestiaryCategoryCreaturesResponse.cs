#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Bestiary
{
    public sealed class BestiaryCategoryCreaturesResponse
    {
        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("categorySlug")]
        public string CategorySlug { get; set; } = string.Empty;

        [JsonPropertyName("className")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("classSlug")]
        public string ClassSlug { get; set; } = string.Empty;

        [JsonPropertyName("creatureCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CreatureCount { get; set; }

        [JsonPropertyName("creatures")]
        public List<BestiaryCreatureListItemResponse> Creatures { get; set; } = [];
    }
}
