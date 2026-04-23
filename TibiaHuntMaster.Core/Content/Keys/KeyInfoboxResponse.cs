#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Keys
{
    public sealed class KeyInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("actualName")]
        public string? ActualName { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("quest")]
        public string? Quest { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("history")]
        public string? History { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("aka")]
        public string? Aka { get; set; }

        [JsonPropertyName("origin")]
        public string? Origin { get; set; }

        [JsonPropertyName("shortNotes")]
        public string? ShortNotes { get; set; }

        [JsonPropertyName("longNotes")]
        public string? LongNotes { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
