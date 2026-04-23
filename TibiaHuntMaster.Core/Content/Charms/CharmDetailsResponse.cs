#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Charms
{
    public sealed class CharmDetailsResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("actualName")]
        public string? ActualName { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("cost")]
        public string Cost { get; set; } = string.Empty;

        [JsonPropertyName("effect")]
        public string Effect { get; set; } = string.Empty;

        [JsonPropertyName("implemented")]
        public string Implemented { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("history")]
        public string? History { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("plainTextContent")]
        public string? PlainTextContent { get; set; }

        [JsonPropertyName("rawWikiText")]
        public string? RawWikiText { get; set; }

        [JsonPropertyName("structuredData")]
        public CharmStructuredDataResponse? StructuredData { get; set; }

        [JsonPropertyName("wikiUrl")]
        public string? WikiUrl { get; set; }

        [JsonPropertyName("lastSeenAt")]
        public DateTimeOffset? LastSeenAt { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
