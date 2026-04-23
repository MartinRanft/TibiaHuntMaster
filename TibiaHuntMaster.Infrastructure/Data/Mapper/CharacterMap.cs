using System.Linq.Expressions;

using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Mapper
{
    public static class CharacterMap
    {
        // ---------- Root: Projection für Linq ----------
        public static Expression<Func<CharacterEntity, Character>> ToDomainProjection => e => new Character
        {
            Id = e.Id,
            Name = e.Name,
            World = e.World,
            Vocation = e.Vocation,
            Level = e.Level,
            GuildName = e.GuildName,
            Residence = e.Residence,
            Title = e.Title,
            AccountStatus = e.AccountStatus,
            AchievementPoints = e.AchievementPoints,
            Sex = e.Sex,
            LastLogin = e.LastLogin,
            LastUpdated = e.LastUpdatedUtc
        };

        // ---------- Root: Domain -> Entity ----------
        public static CharacterEntity ToEntity(Character c)
        {
            return new CharacterEntity
            {
                Id = c.Id,
                Name = c.Name,
                World = c.World,
                Vocation = c.Vocation,
                Level = c.Level,
                GuildName = c.GuildName,
                Residence = c.Residence,
                Title = c.Title,
                AccountStatus = c.AccountStatus,
                AchievementPoints = c.AchievementPoints,
                Sex = c.Sex,
                LastLogin = c.LastLogin,
                LastUpdatedUtc = c.LastUpdated
            };
        }

        // ---------- Root: Entity -> Domain ----------
        public static Character ToDomain(CharacterEntity e)
        {
            return new Character
            {
                Id = e.Id,
                Name = e.Name,
                World = e.World,
                Vocation = e.Vocation,
                Level = e.Level,
                GuildName = e.GuildName,
                Residence = e.Residence,
                Title = e.Title,
                AccountStatus = e.AccountStatus,
                AchievementPoints = e.AchievementPoints,
                Sex = e.Sex,
                LastLogin = e.LastLogin,
                LastUpdated = e.LastUpdatedUtc,

                // Children nur befüllen, wenn Include geladen
                Badges = e.Badges?.Select(ToDomain).ToList() ?? new List<AccountBadge>(),
                Achievements = e.Achievements?.Select(ToDomain).ToList() ?? new List<Achievement>(),
                Houses = e.Houses?.Select(ToDomain).ToList() ?? new List<House>(),
                Deaths = e.Deaths?.Select(ToDomain).ToList() ?? new List<Death>(),
                Account = e.Account is null ? null : ToDomain(e.Account)
            };
        }

        // ---------- Children: Domain -> Entity (Builder) ----------
        public static IEnumerable<CharacterBadgeEntity> ToBadgeEntities(Character c)
        {
            if(c.Badges is null)
            {
                yield break;
            }
            foreach(AccountBadge b in c.Badges)
            {
                yield return new CharacterBadgeEntity
                {
                    Name = b.Name,
                    Description = b.Description,
                    IconUrl = b.IconUrl
                    // kein UpdatedAtUtc, weil in deinen Entities offenbar nicht vorhanden
                };
            }
        }

        public static IEnumerable<CharacterAchievementEntity> ToAchievementEntities(Character c)
        {
            if(c.Achievements is null)
            {
                yield break;
            }
            foreach(Achievement a in c.Achievements)
            {
                // Domain: string Grade  -> Entity: int Grade
                int gradeInt = 0;
                _ = int.TryParse(a.Grade, out gradeInt);

                yield return new CharacterAchievementEntity
                {
                    Name = a.Name,
                    Grade = gradeInt, // konvertiert robust
                    Secret = a.Secret
                    // kein UpdatedAtUtc
                };
            }
        }

        public static IEnumerable<CharacterHouseEntity> ToHouseEntities(Character c)
        {
            if(c.Houses is null)
            {
                yield break;
            }
            foreach(House h in c.Houses)
            {
                yield return new CharacterHouseEntity
                {
                    HouseId = h.HouseId,
                    Name = h.Name,
                    Town = h.Town,
                    Paid = h.Paid
                    // kein UpdatedAtUtc
                };
            }
        }

        public static IEnumerable<CharacterDeathEntity> ToDeathEntities(Character c)
        {
            if(c.Deaths is null)
            {
                yield break;
            }
            foreach(Death d in c.Deaths)
            {
                yield return new CharacterDeathEntity
                {
                    TimeUtc = d.TimeUtc,
                    Level = d.Level,
                    Reason = d.Reason,
                    KillersJson = string.IsNullOrWhiteSpace(d.KillersJson) ? "[]" : d.KillersJson
                    // kein UpdatedAtUtc
                };
            }
        }

        public static CharacterAccountEntity? ToAccountEntityOrNull(Character c)
        {
            if(c.Account is null)
            {
                return null;
            }
            return new CharacterAccountEntity
            {
                Created = c.Account.Created,
                LoyaltyTitle = c.Account.LoyaltyTitle,
                Position = c.Account.Position
                // kein UpdatedAtUtc
            };
        }

        // ---------- Children: Entity -> Domain ----------
        private static AccountBadge ToDomain(CharacterBadgeEntity e)
        {
            return new AccountBadge
            {
                Name = e.Name,
                Description = e.Description,
                IconUrl = e.IconUrl
            };
        }

        private static Achievement ToDomain(CharacterAchievementEntity e)
        {
            return new Achievement
            {
                Name = e.Name,
                Grade = e.Grade.ToString(), // Entity int -> Domain string
                Secret = e.Secret
            };
        }

        private static House ToDomain(CharacterHouseEntity e)
        {
            return new House
            {
                HouseId = e.HouseId,
                Name = e.Name,
                Town = e.Town,
                Paid = e.Paid
            };
        }

        private static Death ToDomain(CharacterDeathEntity e)
        {
            return new Death
            {
                TimeUtc = e.TimeUtc,
                Level = e.Level,
                Reason = e.Reason,
                KillersJson = e.KillersJson
            };
        }

        private static AccountInfo ToDomain(CharacterAccountEntity e)
        {
            return new AccountInfo
            {
                Created = e.Created,
                LoyaltyTitle = e.LoyaltyTitle,
                Position = e.Position
            };
        }
    }
}