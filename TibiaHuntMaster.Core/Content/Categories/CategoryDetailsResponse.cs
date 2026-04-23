#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Categories
{
    public sealed class CategoryDetailsResponse
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

        [JsonPropertyName("sourceKind")]
        public string SourceKind { get; set; } = string.Empty;

        [JsonPropertyName("sourceTitle")]
        public string SourceTitle { get; set; } = string.Empty;

        [JsonPropertyName("sourceSection")]
        public string? SourceSection { get; set; }

        [JsonPropertyName("objectClass")]
        public string? ObjectClass { get; set; }

        [JsonPropertyName("sortOrder")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int SortOrder { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonPropertyName("itemCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int ItemCount { get; set; }

        [JsonPropertyName("wikiArticleCount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int WikiArticleCount { get; set; }
    }
}
