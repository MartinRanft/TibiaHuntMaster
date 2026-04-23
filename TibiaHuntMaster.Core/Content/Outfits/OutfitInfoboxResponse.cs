#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Outfits
{
    public sealed class OutfitInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("outfit")]
        public string? Outfit { get; set; }

        [JsonPropertyName("primaryType")]
        public string? PrimaryType { get; set; }

        [JsonPropertyName("secondaryType")]
        public string? SecondaryType { get; set; }

        [JsonPropertyName("maleId")]
        public string? MaleId { get; set; }

        [JsonPropertyName("femaleId")]
        public string? FemaleId { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("addons")]
        public string? Addons { get; set; }

        [JsonPropertyName("premium")]
        public string? Premium { get; set; }

        [JsonPropertyName("artwork")]
        public string? Artwork { get; set; }

        [JsonPropertyName("bought")]
        public string? Bought { get; set; }

        [JsonPropertyName("achievement")]
        public string? Achievement { get; set; }

        [JsonPropertyName("baseOutfitPrice")]
        public string? BaseOutfitPrice { get; set; }

        [JsonPropertyName("fullOutfitPrice")]
        public string? FullOutfitPrice { get; set; }

        [JsonPropertyName("addon1Price")]
        public string? Addon1Price { get; set; }

        [JsonPropertyName("addon2Price")]
        public string? Addon2Price { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("history")]
        public string? History { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
