#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Locations
{
    public sealed class LocationInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("ruler")]
        public string? Ruler { get; set; }

        [JsonPropertyName("population")]
        public string? Population { get; set; }

        [JsonPropertyName("organization")]
        public string? Organization { get; set; }

        [JsonPropertyName("organizations")]
        public string? Organizations { get; set; }

        [JsonPropertyName("links")]
        public string? Links { get; set; }

        [JsonPropertyName("near")]
        public string? Near { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("seeAlso")]
        public string? SeeAlso { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("map")]
        public string? Map { get; set; }

        [JsonPropertyName("map2")]
        public string? Map2 { get; set; }

        [JsonPropertyName("map3")]
        public string? Map3 { get; set; }

        [JsonPropertyName("map4")]
        public string? Map4 { get; set; }

        [JsonPropertyName("map6")]
        public string? Map6 { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
