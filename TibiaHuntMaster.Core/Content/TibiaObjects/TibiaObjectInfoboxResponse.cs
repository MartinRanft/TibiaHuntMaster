#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.TibiaObjects
{
    public sealed class TibiaObjectInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("actualName")]
        public string? ActualName { get; set; }

        [JsonPropertyName("article")]
        public string? Article { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("itemId")]
        public string? ItemId { get; set; }

        [JsonPropertyName("primaryType")]
        public string? PrimaryType { get; set; }

        [JsonPropertyName("secondaryType")]
        public string? SecondaryType { get; set; }

        [JsonPropertyName("objectClass")]
        public string? ObjectClass { get; set; }

        [JsonPropertyName("pickupable")]
        public string? Pickupable { get; set; }

        [JsonPropertyName("immobile")]
        public string? Immobile { get; set; }

        [JsonPropertyName("walkable")]
        public string? Walkable { get; set; }

        [JsonPropertyName("droppedBy")]
        public string? DroppedBy { get; set; }

        [JsonPropertyName("sellTo")]
        public string? SellTo { get; set; }

        [JsonPropertyName("buyFrom")]
        public string? BuyFrom { get; set; }

        [JsonPropertyName("npcPrice")]
        public string? NpcPrice { get; set; }

        [JsonPropertyName("npcValue")]
        public string? NpcValue { get; set; }

        [JsonPropertyName("value")]
        public string? ValueValue { get; set; }

        [JsonPropertyName("weight")]
        public string? Weight { get; set; }

        [JsonPropertyName("marketable")]
        public string? Marketable { get; set; }

        [JsonPropertyName("stackable")]
        public string? Stackable { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
