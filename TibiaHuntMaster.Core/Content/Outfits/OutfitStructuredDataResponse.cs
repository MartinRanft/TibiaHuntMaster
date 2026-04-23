#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Outfits
{
    public sealed class OutfitStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public OutfitInfoboxResponse? Infobox { get; set; }
    }
}
