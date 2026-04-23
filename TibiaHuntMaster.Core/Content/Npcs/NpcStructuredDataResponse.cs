#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Npcs
{
    public sealed class NpcStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public NpcInfoboxResponse? Infobox { get; set; }
    }
}
