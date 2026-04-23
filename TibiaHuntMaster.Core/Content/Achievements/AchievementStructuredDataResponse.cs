#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Achievements
{
    public sealed class AchievementStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public AchievementInfoboxResponse? Infobox { get; set; }
    }
}
