#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Missiles
{
    public sealed class MissileInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("primaryType")]
        public string? PrimaryType { get; set; }

        [JsonPropertyName("secondaryType")]
        public string? SecondaryType { get; set; }

        [JsonPropertyName("shotBy")]
        public string? ShotBy { get; set; }

        [JsonPropertyName("missileId")]
        public string? MissileId { get; set; }

        [JsonPropertyName("lightRadius")]
        public string? LightRadius { get; set; }

        [JsonPropertyName("lightColor")]
        public string? LightColor { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
