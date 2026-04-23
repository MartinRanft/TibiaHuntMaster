#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Quests
{
    public sealed class QuestRewardResponse
    {
        [JsonPropertyName("index")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Index { get; set; }

        [JsonPropertyName("value")]
        public string ValueValue { get; set; } = string.Empty;
    }
}
