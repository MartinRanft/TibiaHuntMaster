#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Corpses
{
    public sealed class CorpseInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("actualName")]
        public string? ActualName { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("article")]
        public string? Article { get; set; }

        [JsonPropertyName("corpseOf")]
        public string? CorpseOf { get; set; }

        [JsonPropertyName("liquid")]
        public string? Liquid { get; set; }

        [JsonPropertyName("skinable")]
        public string? Skinable { get; set; }

        [JsonPropertyName("product")]
        public string? Product { get; set; }

        [JsonPropertyName("sellTo")]
        public string? SellTo { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("flavourText")]
        public string? FlavourText { get; set; }

        [JsonPropertyName("decayTime1")]
        public string? DecayTime1 { get; set; }

        [JsonPropertyName("decayTime2")]
        public string? DecayTime2 { get; set; }

        [JsonPropertyName("decayTime3")]
        public string? DecayTime3 { get; set; }

        [JsonPropertyName("volume1")]
        public string? Volume1 { get; set; }

        [JsonPropertyName("volume2")]
        public string? Volume2 { get; set; }

        [JsonPropertyName("volume3")]
        public string? Volume3 { get; set; }

        [JsonPropertyName("weight1")]
        public string? Weight1 { get; set; }

        [JsonPropertyName("weight2")]
        public string? Weight2 { get; set; }

        [JsonPropertyName("weight3")]
        public string? Weight3 { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
