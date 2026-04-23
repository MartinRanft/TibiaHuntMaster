#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Mounts
{
    public sealed class MountInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("actualName")]
        public string? ActualName { get; set; }

        [JsonPropertyName("mountId")]
        public string? MountId { get; set; }

        [JsonPropertyName("tamingMethod")]
        public string? TamingMethod { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("speed")]
        public string? Speed { get; set; }

        [JsonPropertyName("bought")]
        public string? Bought { get; set; }

        [JsonPropertyName("price")]
        public string? Price { get; set; }

        [JsonPropertyName("achievement")]
        public string? Achievement { get; set; }

        [JsonPropertyName("tournament")]
        public string? Tournament { get; set; }

        [JsonPropertyName("colourisable")]
        public string? Colourisable { get; set; }

        [JsonPropertyName("artwork")]
        public string? Artwork { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("history")]
        public string? History { get; set; }

        [JsonPropertyName("lightColor")]
        public string? LightColor { get; set; }

        [JsonPropertyName("lightRadius")]
        public string? LightRadius { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
