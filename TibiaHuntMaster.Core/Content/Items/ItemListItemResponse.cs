#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Items
{
    public sealed class ItemListItemResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("categorySlug")]
        public string? CategorySlug { get; set; }

        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("primaryType")]
        public string? PrimaryType { get; set; }

        [JsonPropertyName("secondaryType")]
        public string? SecondaryType { get; set; }

        [JsonPropertyName("objectClass")]
        public string? ObjectClass { get; set; }

        [JsonPropertyName("wikiUrl")]
        public string? WikiUrl { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonPropertyName("primaryImage")]
        public ItemImageResponse? PrimaryImage { get; set; }
    }
}
