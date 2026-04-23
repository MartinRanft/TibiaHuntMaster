#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Achievements
{
    public sealed class AchievementDetailsResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("actualName")]
        public string? ActualName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("spoiler")]
        public string? Spoiler { get; set; }

        [JsonPropertyName("grade")]
        public string? Grade { get; set; }

        [JsonPropertyName("points")]
        public string? Points { get; set; }

        [JsonPropertyName("premium")]
        public string? Premium { get; set; }

        [JsonPropertyName("secret")]
        public string? Secret { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("achievementId")]
        public string? AchievementId { get; set; }

        [JsonPropertyName("relatedPages")]
        public string? RelatedPages { get; set; }

        [JsonPropertyName("history")]
        public string? History { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("plainTextContent")]
        public string? PlainTextContent { get; set; }

        [JsonPropertyName("rawWikiText")]
        public string? RawWikiText { get; set; }

        [JsonPropertyName("structuredData")]
        public AchievementStructuredDataResponse? StructuredData { get; set; }

        [JsonPropertyName("wikiUrl")]
        public string? WikiUrl { get; set; }

        [JsonPropertyName("lastSeenAt")]
        public DateTimeOffset? LastSeenAt { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
