#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Keys
{
    public sealed class KeyStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public KeyInfoboxResponse? Infobox { get; set; }
    }
}
