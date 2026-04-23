#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Missiles
{
    public sealed class MissileDetailsResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("plainTextContent")]
        public string? PlainTextContent { get; set; }

        [JsonPropertyName("rawWikiText")]
        public string? RawWikiText { get; set; }

        [JsonPropertyName("structuredData")]
        public MissileStructuredDataResponse? StructuredData { get; set; }

        [JsonPropertyName("wikiUrl")]
        public string? WikiUrl { get; set; }

        [JsonPropertyName("lastSeenAt")]
        public DateTimeOffset? LastSeenAt { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
