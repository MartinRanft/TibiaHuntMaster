#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Effects
{
    public sealed class EffectStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public EffectInfoboxResponse? Infobox { get; set; }
    }
}
