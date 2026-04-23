#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Buildings
{
    public sealed class BuildingInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("street2")]
        public string? Street2 { get; set; }

        [JsonPropertyName("street3")]
        public string? Street3 { get; set; }

        [JsonPropertyName("houseId")]
        public string? HouseId { get; set; }

        [JsonPropertyName("size")]
        public string? Size { get; set; }

        [JsonPropertyName("beds")]
        public string? Beds { get; set; }

        [JsonPropertyName("rent")]
        public string? Rent { get; set; }

        [JsonPropertyName("openWindows")]
        public string? OpenWindows { get; set; }

        [JsonPropertyName("floors")]
        public string? Floors { get; set; }

        [JsonPropertyName("rooms")]
        public string? Rooms { get; set; }

        [JsonPropertyName("furnishings")]
        public string? Furnishings { get; set; }

        [JsonPropertyName("history")]
        public string? History { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("ownable")]
        public string? Ownable { get; set; }

        [JsonPropertyName("positionX")]
        public string? PositionX { get; set; }

        [JsonPropertyName("positionY")]
        public string? PositionY { get; set; }

        [JsonPropertyName("positionZ")]
        public string? PositionZ { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
