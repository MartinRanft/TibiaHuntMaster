#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Loot
{
    public sealed class LootStatisticDetailsResponse
    {
        [JsonPropertyName("creatureId")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CreatureId { get; set; }

        [JsonPropertyName("creatureName")]
        public string CreatureName { get; set; } = string.Empty;

        [JsonPropertyName("lootStatistics")]
        public List<LootStatisticEntryResponse> LootStatistics { get; set; } = [];

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
