#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Buildings
{
    public sealed class BuildingAddressResponse
    {
        [JsonPropertyName("index")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Index { get; set; }

        [JsonPropertyName("street")]
        public string Street { get; set; } = string.Empty;

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }
    }
}
