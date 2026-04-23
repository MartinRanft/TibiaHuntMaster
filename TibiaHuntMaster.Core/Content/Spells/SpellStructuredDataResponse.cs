#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Spells
{
    public sealed class SpellStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public SpellInfoboxResponse? Infobox { get; set; }
    }
}
