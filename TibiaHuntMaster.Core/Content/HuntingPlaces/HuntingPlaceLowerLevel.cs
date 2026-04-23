#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.HuntingPlaces
{
    /// <summary>
    /// DTO representing a lower level area within a hunting place.
    /// </summary>
    public sealed class HuntingPlaceLowerLevel
    {
        [JsonPropertyName("areaname")]
        public string? Areaname { get; set; }

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

        [JsonPropertyName("exp")]
        public string? Exp { get; set; }
    }
}
