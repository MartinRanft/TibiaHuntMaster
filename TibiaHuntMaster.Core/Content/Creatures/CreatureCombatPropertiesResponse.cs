#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Creatures
{
    public sealed class CreatureCombatPropertiesResponse
    {
        [JsonPropertyName("armor")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Armor { get; set; }

        [JsonPropertyName("mitigation")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? Mitigation { get; set; }

        [JsonPropertyName("maxDamage")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? MaxDamage { get; set; }

        [JsonPropertyName("speed")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Speed { get; set; }

        [JsonPropertyName("runsAt")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? RunsAt { get; set; }

        [JsonPropertyName("isBoss")]
        public bool? IsBoss { get; set; }

        [JsonPropertyName("usesSpells")]
        public bool? UsesSpells { get; set; }

        [JsonPropertyName("pushable")]
        public bool? Pushable { get; set; }

        [JsonPropertyName("pushObjects")]
        public bool? PushObjects { get; set; }

        [JsonPropertyName("walksAround")]
        public bool? WalksAround { get; set; }
    }
}
