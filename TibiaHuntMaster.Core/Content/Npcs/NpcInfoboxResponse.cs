#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Npcs
{
    public sealed class NpcInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("actualName")]
        public string? ActualName { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("race")]
        public string? Race { get; set; }

        [JsonPropertyName("job")]
        public string? Job { get; set; }

        [JsonPropertyName("job2")]
        public string? Job2 { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("buySell")]
        public string? BuySell { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("subarea")]
        public string? Subarea { get; set; }

        [JsonPropertyName("sounds")]
        public string? Sounds { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("positionX")]
        public string? PositionX { get; set; }

        [JsonPropertyName("positionY")]
        public string? PositionY { get; set; }

        [JsonPropertyName("positionZ")]
        public string? PositionZ { get; set; }

        [JsonPropertyName("positionX2")]
        public string? PositionX2 { get; set; }

        [JsonPropertyName("positionY2")]
        public string? PositionY2 { get; set; }

        [JsonPropertyName("positionZ2")]
        public string? PositionZ2 { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
