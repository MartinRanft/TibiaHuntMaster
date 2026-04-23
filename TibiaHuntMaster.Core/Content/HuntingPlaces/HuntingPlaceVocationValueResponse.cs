#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.HuntingPlaces
{
    public sealed class HuntingPlaceVocationValueResponse
    {
        [JsonPropertyName("knights")]
        public string? Knights { get; set; }

        [JsonPropertyName("paladins")]
        public string? Paladins { get; set; }

        [JsonPropertyName("mages")]
        public string? Mages { get; set; }
    }
}
