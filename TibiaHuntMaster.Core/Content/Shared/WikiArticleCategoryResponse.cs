#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Shared
{
    public sealed class WikiArticleCategoryResponse
    {
        [JsonPropertyName("categoryId")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int CategoryId { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("groupSlug")]
        public string GroupSlug { get; set; } = string.Empty;

        [JsonPropertyName("groupName")]
        public string GroupName { get; set; } = string.Empty;
    }
}
