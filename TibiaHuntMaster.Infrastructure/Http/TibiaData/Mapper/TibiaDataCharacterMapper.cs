using System.Globalization;
using System.Text.Json;

using TibiaHuntMaster.Core.Characters;

namespace TibiaHuntMaster.Infrastructure.Http.TibiaData.Mapper
{
    internal static class TibiaDataCharacterMapper
    {
        internal static Character ToDomain(TibiaDataCharacterResponse api)
        {
            ArgumentNullException.ThrowIfNull(api);
            if(api.Character == null)
            {
                throw new ArgumentNullException(nameof(api.Character));
            }

            TibiaCharacterDetails c = api.Character.Character;

            Character domainChar = new()
            {
                Name = c.Name,
                World = c.World,
                Vocation = c.Vocation,
                Level = c.Level,
                GuildName = c.Guild?.Name ?? string.Empty,
                Residence = c.Residence,
                Title = c.Title,
                AccountStatus = c.AccountStatus,
                AchievementPoints = c.AchievementPoints,
                Sex = c.Sex,
                LastLogin = ParseDate(c.LastLogin),
                LastUpdated = DateTimeOffset.UtcNow
            };

            // --- 1. Account Information (falls vorhanden) ---
            if(api.Character.AccountInformation != null)
            {
                domainChar.Account = new AccountInfo
                {
                    Created = api.Character.AccountInformation.Created,
                    LoyaltyTitle = api.Character.AccountInformation.LoyaltyTitle,
                    Position = api.Character.AccountInformation.Position
                };
            }

            // --- 2. Deaths ---
            foreach(TibiaDeath d in api.Character.Deaths)
            {
                // TibiaData liefert Zeit als String. Wir versuchen zu parsen.
                // Fallback auf UtcNow, falls Parsing scheitert (sollte aber klappen).
                DateTimeOffset deathTime = ParseDate(d.Time) ?? DateTimeOffset.UtcNow;

                domainChar.Deaths.Add(new Death
                {
                    Level = d.Level,
                    Reason = d.Reason,
                    TimeUtc = deathTime,
                    // Wir speichern die Killer-Liste als rohes JSON, falls wir es später detailliert brauchen
                    KillersJson = JsonSerializer.Serialize(d.Killers)
                });
            }

            // --- 3. Houses ---
            foreach(TibiaHouse h in c.Houses)
            {
                domainChar.Houses.Add(new House
                {
                    HouseId = h.HouseId,
                    Name = h.Name,
                    Town = h.Town,
                    Paid = h.Paid
                });
            }

            // --- 4. Badges ---
            foreach(TibiaAccountBadge b in api.Character.AccountBadges)
            {
                domainChar.Badges.Add(new AccountBadge
                {
                    Name = b.Name,
                    Description = b.Description,
                    IconUrl = b.IconUrl
                });
            }

            // --- 5. Achievements (Die auf der Main Page sichtbaren) ---
            foreach(TibiaAchievement a in api.Character.Achievements)
            {
                domainChar.Achievements.Add(new Achievement
                {
                    Name = a.Name,
                    Grade = a.Grade.ToString(),
                    Secret = a.Secret
                });
            }

            return domainChar;
        }

        private static DateTimeOffset? ParseDate(string? dateString)
        {
            if(string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            string s = dateString.Trim();

            // Zeitzonen fixen (TibiaData liefert oft CEST/CET)
            if(s.EndsWith(" CEST"))
            {
                s = s.Replace(" CEST", " +02:00");
            }
            else if(s.EndsWith(" CET"))
            {
                s = s.Replace(" CET", " +01:00");
            }
            else if(s.EndsWith(" UTC"))
            {
                s = s.Replace(" UTC", "Z");
            }

            if(DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dto))
            {
                return dto;
            }

            return null;
        }
    }
}