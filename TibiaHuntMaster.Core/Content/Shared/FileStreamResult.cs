#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Shared
{
    public sealed class FileStreamResult
    {
        [JsonPropertyName("fileStream")]
        public byte[] FileStream { get; set; } = [];

        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("fileDownloadName")]
        public string? FileDownloadName { get; set; }

        [JsonPropertyName("lastModified")]
        public DateTimeOffset? LastModified { get; set; }

        [JsonPropertyName("entityTag")]
        public EntityTagHeaderValue? EntityTag { get; set; }

        [JsonPropertyName("enableRangeProcessing")]
        public bool EnableRangeProcessing { get; set; }
    }
}
