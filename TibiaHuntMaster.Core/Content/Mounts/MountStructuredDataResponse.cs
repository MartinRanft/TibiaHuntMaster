#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Mounts
{
    public sealed class MountStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public MountInfoboxResponse? Infobox { get; set; }
    }
}
