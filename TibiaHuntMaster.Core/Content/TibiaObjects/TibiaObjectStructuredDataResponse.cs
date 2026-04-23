#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.TibiaObjects
{
    public sealed class TibiaObjectStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public TibiaObjectInfoboxResponse? Infobox { get; set; }
    }
}
