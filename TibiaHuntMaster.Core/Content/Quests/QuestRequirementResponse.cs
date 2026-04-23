#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Quests
{
    public sealed class QuestRequirementResponse
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string ValueValue { get; set; } = string.Empty;
    }
}
