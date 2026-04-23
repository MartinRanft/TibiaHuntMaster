#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Effects
{
    public sealed class EffectInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("primaryType")]
        public string? PrimaryType { get; set; }

        [JsonPropertyName("secondaryType")]
        public string? SecondaryType { get; set; }

        [JsonPropertyName("causes")]
        public string? Causes { get; set; }

        [JsonPropertyName("effectId")]
        public string? EffectId { get; set; }

        [JsonPropertyName("effect")]
        public string? Effect { get; set; }

        [JsonPropertyName("lightColor")]
        public string? LightColor { get; set; }

        [JsonPropertyName("lightRadius")]
        public string? LightRadius { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("history")]
        public string? History { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
