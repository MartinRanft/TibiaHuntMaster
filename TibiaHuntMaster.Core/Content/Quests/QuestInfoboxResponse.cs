#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.Quests
{
    public sealed class QuestInfoboxResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("aka")]
        public string? Aka { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("implemented")]
        public string? Implemented { get; set; }

        [JsonPropertyName("premium")]
        public string? Premium { get; set; }

        [JsonPropertyName("level")]
        public string? Level { get; set; }

        [JsonPropertyName("levelRecommended")]
        public string? LevelRecommended { get; set; }

        [JsonPropertyName("levelNote")]
        public string? LevelNote { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("dangers")]
        public string? Dangers { get; set; }

        [JsonPropertyName("legend")]
        public string? Legend { get; set; }

        [JsonPropertyName("reward")]
        public string? Reward { get; set; }

        [JsonPropertyName("log")]
        public string? Log { get; set; }

        [JsonPropertyName("time")]
        public string? Time { get; set; }

        [JsonPropertyName("timeAllocation")]
        public string? TimeAllocation { get; set; }

        [JsonPropertyName("transcripts")]
        public string? Transcripts { get; set; }

        [JsonPropertyName("rookgaardQuest")]
        public string? RookgaardQuest { get; set; }

        [JsonPropertyName("history")]
        public string? History { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string>? Fields { get; set; }
    }
}
