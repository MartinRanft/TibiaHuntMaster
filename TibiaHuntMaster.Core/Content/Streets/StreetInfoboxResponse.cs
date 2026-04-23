#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Streets
{
    public sealed class StreetInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("actualName")]
        public string? ActualName { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("city2")]
        public string? City2 { get; set; }

        [JsonPropertyName("floor")]
        public string? Floor { get; set; }

        [JsonPropertyName("map")]
        public string? Map { get; set; }

        [JsonPropertyName("style")]
        public string? Style { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
