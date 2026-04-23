#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Achievements
{
    public sealed class AchievementInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

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

        [JsonPropertyName("coincidesWith")]
        public string? CoincidesWith { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
