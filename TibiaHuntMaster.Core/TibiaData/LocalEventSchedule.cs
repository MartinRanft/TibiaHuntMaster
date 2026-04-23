using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.TibiaData
{
    public sealed class LocalEventScheduleRoot
    {
        [JsonPropertyName("eventlist")]public List<LocalEventItem> EventList { get; set; } = new();
    }

    public class LocalEventItem
    {
        [JsonPropertyName("name")]public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]public string Description { get; set; } = string.Empty;

        // Unix Timestamp (Sekunden)
        [JsonPropertyName("startdate")]public long StartDateUnix { get; set; }

        [JsonPropertyName("enddate")]public long EndDateUnix { get; set; }

        [JsonPropertyName("isseasonal")]public bool IsSeasonal { get; set; }
    }
}