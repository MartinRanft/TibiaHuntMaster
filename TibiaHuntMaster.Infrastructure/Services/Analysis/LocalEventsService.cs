using System.Text.Json;

using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.System;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public sealed class EventDetectionResult
    {
        public bool IsDoubleXp { get; set; }

        public bool IsDoubleLoot { get; set; }

        public bool IsRapidRespawn { get; set; }

        public string DetectedEventNames { get; set; } = string.Empty;
    }

    public sealed class LocalEventsService(
        TibiaPathService pathService,
        ILogger<LocalEventsService> logger)
    {
        public async Task<EventDetectionResult> DetectEventsAsync(DateTimeOffset sessionDate)
        {
            EventDetectionResult result = new();
            string? filePath = pathService.GetEventSchedulePath();

            if(string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                logger.LogWarning("eventschedule.json not found. Skipping auto-detection.");
                return result;
            }

            try
            {
                // Datei lesen (FileShare.ReadWrite wichtig, falls Tibia gerade läuft)
                await using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                LocalEventScheduleRoot? data = await JsonSerializer.DeserializeAsync<LocalEventScheduleRoot>(stream);
                if(data?.EventList == null)
                {
                    return result;
                }

                List<string> activeEvents = [];

                // Datum des Hunts in Unix Timestamp wandeln (für Vergleich)
                long sessionUnix = sessionDate.ToUnixTimeSeconds();

                foreach(LocalEventItem evt in data.EventList.Where(evt => sessionUnix >= evt.StartDateUnix && sessionUnix <= evt.EndDateUnix))
                {
                    activeEvents.Add(evt.Name);

                    // Namen matchen (basierend auf deinen JSON Daten)
                    string n = evt.Name.ToLowerInvariant();

                    // "XP/Skill Event" ist der offizielle Name im JSON für Double XP
                    if(n.Contains("xp/skill event") || n.Contains("double xp"))
                    {
                        result.IsDoubleXp = true;
                    }

                    if(n.Contains("rapid respawn"))
                    {
                        result.IsRapidRespawn = true;
                    }

                    // Double Loot heißt oft "Double Loot Event" oder ähnlich
                    if(n.Contains("double loot"))
                    {
                        result.IsDoubleLoot = true;
                    }
                }

                result.DetectedEventNames = string.Join(", ", activeEvents);
                logger.LogInformation("Detected Events for {Date}: {Events}", sessionDate, result.DetectedEventNames);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading eventschedule.json");
            }

            return result;
        }
    }
}