#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Spells
{
    public sealed class SpellInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("premium")]
        public string? Premium { get; set; }

        [JsonPropertyName("subclass")]
        public string? Subclass { get; set; }

        [JsonPropertyName("voc")]
        public string? Voc { get; set; }

        [JsonPropertyName("mana")]
        public string? Mana { get; set; }

        [JsonPropertyName("soul")]
        public string? Soul { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("spellId")]
        public string? SpellId { get; set; }

        [JsonPropertyName("levelRequired")]
        public string? LevelRequired { get; set; }

        [JsonPropertyName("cooldown")]
        public string? Cooldown { get; set; }

        [JsonPropertyName("cooldownGroup")]
        public string? CooldownGroup { get; set; }

        [JsonPropertyName("cooldownGroup2")]
        public string? CooldownGroup2 { get; set; }

        [JsonPropertyName("words")]
        public string? Words { get; set; }

        [JsonPropertyName("effect")]
        public string? Effect { get; set; }

        [JsonPropertyName("damageType")]
        public string? DamageType { get; set; }

        [JsonPropertyName("animation")]
        public string? Animation { get; set; }

        [JsonPropertyName("basePower")]
        public string? BasePower { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("history")]
        public string? History { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
