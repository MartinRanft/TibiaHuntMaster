#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Missiles
{
    public sealed class MissileStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public MissileInfoboxResponse? Infobox { get; set; }
    }
}
