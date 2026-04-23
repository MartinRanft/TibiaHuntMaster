#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Locations
{
    public sealed class LocationStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public LocationInfoboxResponse? Infobox { get; set; }
    }
}
