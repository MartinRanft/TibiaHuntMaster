#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.HuntingPlaces
{
    /// <summary>
    /// DTO representing the InfoboxJson structure for hunting places stored in the database.
    /// </summary>
    public sealed class HuntingPlaceInfobox
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("vocation")]
        public string? Vocation { get; set; }

        [JsonPropertyName("lvlknights")]
        public string? Lvlknights { get; set; }

        [JsonPropertyName("lvlpaladins")]
        public string? Lvlpaladins { get; set; }

        [JsonPropertyName("lvlmages")]
        public string? Lvlmages { get; set; }

        [JsonPropertyName("skknights")]
        public string? Skknights { get; set; }

        [JsonPropertyName("skpaladins")]
        public string? Skpaladins { get; set; }

        [JsonPropertyName("skmages")]
        public string? Skmages { get; set; }

        [JsonPropertyName("defknights")]
        public string? Defknights { get; set; }

        [JsonPropertyName("defpaladins")]
        public string? Defpaladins { get; set; }

        [JsonPropertyName("defmages")]
        public string? Defmages { get; set; }

        [JsonPropertyName("loot")]
        public string? Loot { get; set; }

        [JsonPropertyName("lootstar")]
        public string? Lootstar { get; set; }

        [JsonPropertyName("exp")]
        public string? Exp { get; set; }

        [JsonPropertyName("expstar")]
        public string? Expstar { get; set; }

        [JsonPropertyName("bestloot")]
        public string? Bestloot { get; set; }

        [JsonPropertyName("bestloot2")]
        public string? Bestloot2 { get; set; }

        [JsonPropertyName("bestloot3")]
        public string? Bestloot3 { get; set; }

        [JsonPropertyName("bestloot4")]
        public string? Bestloot4 { get; set; }

        [JsonPropertyName("bestloot5")]
        public string? Bestloot5 { get; set; }

        [JsonPropertyName("map")]
        public string? Map { get; set; }

        [JsonPropertyName("map2")]
        public string? Map2 { get; set; }

        [JsonPropertyName("map3")]
        public string? Map3 { get; set; }

        [JsonPropertyName("map4")]
        public string? Map4 { get; set; }

        [JsonPropertyName("lowerlevels")]
        public string? Lowerlevels { get; set; }

        [JsonPropertyName("areaname")]
        public string? Areaname { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
