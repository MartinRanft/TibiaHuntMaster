#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Loot
{
    public sealed class LootStatisticEntryResponse
    {
        [JsonPropertyName("itemName")]
        public string? ItemName { get; set; }

        [JsonPropertyName("chance")]
        public string? Chance { get; set; }

        [JsonPropertyName("rarity")]
        public string? Rarity { get; set; }

        [JsonPropertyName("raw")]
        public string? Raw { get; set; }
    }
}
