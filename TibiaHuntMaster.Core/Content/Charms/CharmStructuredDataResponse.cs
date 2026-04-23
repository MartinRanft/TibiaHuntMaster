#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Charms
{
    public sealed class CharmStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public CharmInfoboxResponse? Infobox { get; set; }
    }
}
