#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Shared
{
    public sealed class ProblemDetails
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("status")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Status { get; set; }

        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        [JsonPropertyName("instance")]
        public string? Instance { get; set; }
    }
}
