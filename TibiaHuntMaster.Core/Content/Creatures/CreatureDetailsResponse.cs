#nullable enable
using System.Text.Json.Serialization;

using TibiaHuntMaster.Core.Content.Loot;

namespace TibiaHuntMaster.Core.Content.Creatures
{
    public sealed class CreatureDetailsResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("hitpoints")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Hitpoints { get; set; }

        [JsonPropertyName("experience")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Experience { get; set; }

        [JsonPropertyName("structuredData")]
        public CreatureStructuredDataResponse? StructuredData { get; set; }

        [JsonPropertyName("lootStatistics")]
        public List<LootStatisticEntryResponse> LootStatistics { get; set; } = [];

        [JsonPropertyName("images")]
        public List<CreatureImageResponse> Images { get; set; } = [];

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
