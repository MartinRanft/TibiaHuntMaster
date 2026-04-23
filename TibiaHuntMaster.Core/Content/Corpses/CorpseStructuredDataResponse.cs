#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Corpses
{
    public sealed class CorpseStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public CorpseInfoboxResponse? Infobox { get; set; }
    }
}
