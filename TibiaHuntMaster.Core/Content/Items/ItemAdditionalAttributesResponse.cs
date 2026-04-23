#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Items
{
    public sealed class ItemAdditionalAttributesResponse
    {
        [JsonPropertyName("entries")]
        public List<ItemAttributeEntryResponse> Entries { get; set; } = [];
    }
}
