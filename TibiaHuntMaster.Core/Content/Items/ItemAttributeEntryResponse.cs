#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Items
{
    public sealed class ItemAttributeEntryResponse
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string ValueValue { get; set; } = string.Empty;
    }
}
