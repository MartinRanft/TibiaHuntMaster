using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Creatures
{
    public sealed class TibiaDataCreaturesResponse
    {
        [JsonPropertyName("creatures")]
        public TibiaDataCreaturesContainer Creatures { get; set; } = new();
    }

    public sealed class TibiaDataCreaturesContainer
    {
        [JsonPropertyName("boosted")]
        public TibiaDataCreatureEntry Boosted { get; set; } = new();

        [JsonPropertyName("creature_list")]
        public List<TibiaDataCreatureEntry> CreatureList { get; set; } = [];
    }

    public sealed class TibiaDataCreatureEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("race")]
        public string Race { get; set; } = string.Empty;

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("featured")]
        public bool Featured { get; set; }
    }
}
