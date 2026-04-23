#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Categories
{
    public sealed class CategoryListItemResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("groupSlug")]
        public string GroupSlug { get; set; } = string.Empty;

        [JsonPropertyName("groupName")]
        public string GroupName { get; set; } = string.Empty;

        [JsonPropertyName("objectClass")]
        public string? ObjectClass { get; set; }

        [JsonPropertyName("sortOrder")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int SortOrder { get; set; }
    }
}
