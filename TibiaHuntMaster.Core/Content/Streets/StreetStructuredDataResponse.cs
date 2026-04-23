#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Streets
{
    public sealed class StreetStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public StreetInfoboxResponse? Infobox { get; set; }
    }
}
