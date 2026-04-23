#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Creatures
{
    public sealed class CreatureResistanceSummaryResponse
    {
        [JsonPropertyName("physicalPercent")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? PhysicalPercent { get; set; }

        [JsonPropertyName("earthPercent")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? EarthPercent { get; set; }

        [JsonPropertyName("firePercent")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? FirePercent { get; set; }

        [JsonPropertyName("deathPercent")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? DeathPercent { get; set; }

        [JsonPropertyName("energyPercent")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? EnergyPercent { get; set; }

        [JsonPropertyName("holyPercent")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? HolyPercent { get; set; }

        [JsonPropertyName("icePercent")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? IcePercent { get; set; }

        [JsonPropertyName("lifeDrainPercent")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? LifeDrainPercent { get; set; }

        [JsonPropertyName("drownPercent")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? DrownPercent { get; set; }

        [JsonPropertyName("healingPercent")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? HealingPercent { get; set; }
    }
}
