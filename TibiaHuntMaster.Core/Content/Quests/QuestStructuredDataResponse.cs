#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Quests
{
    public sealed class QuestStructuredDataResponse
    {
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("infobox")]
        public QuestInfoboxResponse? Infobox { get; set; }

        [JsonPropertyName("requirements")]
        public List<QuestRequirementResponse> Requirements { get; set; } = [];

        [JsonPropertyName("rewards")]
        public List<QuestRewardResponse> Rewards { get; set; } = [];
    }
}
