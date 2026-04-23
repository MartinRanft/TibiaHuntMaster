#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.HuntingPlaces
{
    public sealed class HuntingPlaceStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public HuntingPlaceInfobox? Infobox { get; set; }

        [JsonPropertyName("additionalAttributes")]
        public HuntingPlaceAdditionalAttributes? AdditionalAttributes { get; set; }

        [JsonPropertyName("areaCreatureSummaries")]
        public List<HuntingPlaceAreaCreatureSummaryResponse> AreaCreatureSummaries { get; set; } = [];
    }
}
