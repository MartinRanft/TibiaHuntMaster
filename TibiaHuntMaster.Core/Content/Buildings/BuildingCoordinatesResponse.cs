#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Buildings
{
    public sealed class BuildingCoordinatesResponse
    {
        [JsonPropertyName("x")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? X { get; set; }

        [JsonPropertyName("y")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? Y { get; set; }

        [JsonPropertyName("z")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Z { get; set; }
    }
}
