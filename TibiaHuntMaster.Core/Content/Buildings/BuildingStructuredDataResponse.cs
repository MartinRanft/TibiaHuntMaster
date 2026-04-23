#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Buildings
{
    public sealed class BuildingStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public BuildingInfoboxResponse? Infobox { get; set; }

        [JsonPropertyName("addresses")]
        public List<BuildingAddressResponse> Addresses { get; set; } = [];

        [JsonPropertyName("coordinates")]
        public BuildingCoordinatesResponse? Coordinates { get; set; }
    }
}
