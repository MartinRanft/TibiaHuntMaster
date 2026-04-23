using System.Diagnostics;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using TibiaHuntMaster.Core.Abstractions.TibiaData;
using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Core.Security;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;
using TibiaHuntMaster.Infrastructure.Data.Mapper;
using TibiaHuntMaster.Infrastructure.Http.TibiaData;
using TibiaHuntMaster.Infrastructure.Http.TibiaData.Mapper;

namespace TibiaHuntMaster.Infrastructure.Services.TibiaData
{
    public sealed class CharacterService(TibiaDataClient client, IDbContextFactory<AppDbContext> dbFactory) : ICharacterService
    {
        public async Task<Character> ImportFromTibiaDataAsync(string characterName, CancellationToken ct = default)
        {
            string safeCharacterName = UserInputSanitizer.TrimAndTruncate(characterName, UserInputLimits.CharacterNameMaxLength);
            if(string.IsNullOrWhiteSpace(safeCharacterName))
            {
                throw new ArgumentException("Character name is required.", nameof(characterName));
            }

            TibiaDataCharacterResponse api = await client.GetCharactersAsync(safeCharacterName, ct)
                                             ?? throw new Exception("Character not found");

            Character domain = TibiaDataCharacterMapper.ToDomain(api);

            string rawJson = JsonSerializer.Serialize(api);
            await SaveSnapshotAsync(domain, rawJson, ct);

            return domain;
        }

        public async Task SaveAsync(Character character, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(character);
            Normalize(character);
            if(character.LastUpdated == default)
            {
                character.LastUpdated = DateTimeOffset.UtcNow;
            }

            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
            await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);

            CharacterEntity? existing = await db.Characters
                                                .Include(c => c.Badges)
                                                .Include(c => c.Achievements)
                                                .Include(c => c.Houses)
                                                .Include(c => c.Deaths)
                                                .Include(c => c.Account)
                                                .FirstOrDefaultAsync(x => x.Name == character.Name && x.World == character.World, ct);

            if(existing is null)
            {
                CharacterEntity root = CharacterMap.ToEntity(character);

                foreach(CharacterBadgeEntity b in CharacterMap.ToBadgeEntities(character))
                {
                    root.Badges.Add(b);
                }
                foreach(CharacterAchievementEntity a in CharacterMap.ToAchievementEntities(character))
                {
                    root.Achievements.Add(a);
                }
                foreach(CharacterHouseEntity h in CharacterMap.ToHouseEntities(character))
                {
                    root.Houses.Add(h);
                }
                foreach(CharacterDeathEntity d in CharacterMap.ToDeathEntities(character))
                {
                    root.Deaths.Add(d);
                }
                CharacterAccountEntity? acc = CharacterMap.ToAccountEntityOrNull(character);
                if(acc is not null)
                {
                    root.Account = acc;
                }

                if(root.LastUpdatedUtc == default)
                {
                    root.LastUpdatedUtc = DateTimeOffset.UtcNow;
                }

                db.Characters.Add(root);
                await db.SaveChangesAsync(ct);

                character.Id = root.Id;
                await tx.CommitAsync(ct);
                return;
            }

            ApplyScalarUpdates(existing, character);
            existing.LastUpdatedUtc = DateTimeOffset.UtcNow;

            SyncByKey(existing.Badges,
                CharacterMap.ToBadgeEntities(character),
                b => b.Name,
                (cur, inc) =>
                {
                    cur.Description = inc.Description;
                    cur.IconUrl = inc.IconUrl;
                });
            SyncByKey(existing.Achievements,
                CharacterMap.ToAchievementEntities(character),
                a => a.Name,
                (cur, inc) =>
                {
                    cur.Grade = inc.Grade;
                    cur.Secret = inc.Secret;
                });
            SyncByKey(existing.Houses,
                CharacterMap.ToHouseEntities(character),
                h => h.HouseId,
                (cur, inc) =>
                {
                    cur.Name = inc.Name;
                    cur.Town = inc.Town;
                    cur.Paid = inc.Paid;
                });
            SyncByKey(existing.Deaths,
                CharacterMap.ToDeathEntities(character),
                d => d.TimeUtc,
                (cur, inc) =>
                {
                    cur.Level = inc.Level;
                    cur.Reason = inc.Reason;
                    cur.KillersJson = inc.KillersJson;
                });

            CharacterAccountEntity? incomingAccount = CharacterMap.ToAccountEntityOrNull(character);
            if(existing.Account is null && incomingAccount is not null)
            {
                existing.Account = incomingAccount;
            }
            else if(existing.Account is not null && incomingAccount is not null)
            {
                existing.Account.Created = incomingAccount.Created;
                existing.Account.LoyaltyTitle = incomingAccount.LoyaltyTitle;
                existing.Account.Position = incomingAccount.Position;
            }
            else if(existing.Account is not null && incomingAccount is null)
            {
                db.CharacterAccounts.Remove(existing.Account);
                existing.Account = null!;
            }

            await db.SaveChangesAsync(ct);
            character.Id = existing.Id;
            await tx.CommitAsync(ct);
        }

        public async Task<IReadOnlyList<Character>> ListAsync(CancellationToken ct = default)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
            return await db.Characters
                           .AsNoTracking()
                           .OrderBy(x => x.Name)
                           .Select(CharacterMap.ToDomainProjection)
                           .ToListAsync(ct);
        }

        public async Task<Character?> GetByNameAsync(string name, CancellationToken ct = default)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
            string searchName = UserInputSanitizer.TrimAndTruncate(name, UserInputLimits.CharacterNameMaxLength);
            if(string.IsNullOrWhiteSpace(searchName))
            {
                return null;
            }

            try
            {
                CharacterEntity? entity = await db.Characters
                                                  .AsNoTracking()
                                                  .Include(c => c.Deaths)
                                                  .Include(c => c.Badges)
                                                  .Include(c => c.Houses)
                                                  .Include(c => c.Achievements)
                                                  .Include(c => c.Account)
                                                  // Robusterer Vergleich für SQLite (Case Insensitive)
                                                  .Where(x => EF.Functions.Like(x.Name, searchName))
                                                  .OrderByDescending(x => x.LastUpdatedUtc)
                                                  .FirstOrDefaultAsync(ct);

                return entity == null ? null : CharacterMap.ToDomain(entity);
            }
            catch (Exception ex)
            {
                // Hier siehst du, ob es ein DB-Fehler (Mapping) ist
                Debug.WriteLine($"DB ERROR GetByName: {ex.Message}");
                return null;
            }
        }

        private async Task SaveSnapshotAsync(Character character, string rawJson, CancellationToken ct)
        {
            // Rekursiver Aufruf: SaveAsync nutzt intern seine eigene Factory-Instanz, das ist ok.
            await SaveAsync(character, ct);

            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

            int charId = character.Id;
            if(charId <= 0)
            {
                charId = await db.Characters
                                 .Where(c => c.Name == character.Name && c.World == character.World)
                                 .Select(c => c.Id)
                                 .SingleAsync(ct);
            }

            CharacterSnapshotEntity? last = await db.CharacterSnapshots
                                                    .Where(s => s.CharacterId == charId)
                                                    .OrderByDescending(s => s.FetchedAtUtc)
                                                    .FirstOrDefaultAsync(ct);

            if(last is not null && string.Equals(last.RawJson, rawJson, StringComparison.Ordinal))
            {
                return;
            }

            db.CharacterSnapshots.Add(new CharacterSnapshotEntity
            {
                CharacterId = charId,
                FetchedAtUtc = DateTimeOffset.UtcNow,
                RawJson = rawJson
            });
            await db.SaveChangesAsync(ct);

            List<int> surplusIds = await db.CharacterSnapshots
                                           .Where(s => s.CharacterId == charId)
                                           .OrderByDescending(s => s.FetchedAtUtc)
                                           .Skip(30)
                                           .Select(s => s.Id)
                                           .ToListAsync(ct);

            if(surplusIds.Count > 0)
            {
                // SQLite hat kein ExecuteDeleteAsync in älteren EF Versionen, aber EF10 hat es. 
                // Wir bleiben beim sicheren RemoveRange für Kompatibilität.
                List<CharacterSnapshotEntity> toRemove = surplusIds.Select(id => new CharacterSnapshotEntity
                {
                    Id = id
                }).ToList();
                db.RemoveRange(toRemove);
                await db.SaveChangesAsync(ct);
            }
        }

        private static void Normalize(Character c)
        {
            c.Name = UserInputSanitizer.TrimAndTruncate(c.Name, UserInputLimits.CharacterNameMaxLength);
            c.World = UserInputSanitizer.TrimAndTruncate(c.World, 64);
            c.Vocation = UserInputSanitizer.TrimAndTruncate(c.Vocation, 64);
            c.GuildName = UserInputSanitizer.TrimAndTruncate(c.GuildName, 100);
            c.Residence = UserInputSanitizer.TrimAndTruncate(c.Residence, 64);
            c.Title = UserInputSanitizer.TrimAndTruncate(c.Title, 100);
            c.AccountStatus = UserInputSanitizer.TrimAndTruncate(c.AccountStatus, 50);
            c.Sex = UserInputSanitizer.TrimAndTruncate(c.Sex, 16);

            if(c.Account != null)
            {
                c.Account.Created = UserInputSanitizer.TrimAndTruncate(c.Account.Created, 64);
                c.Account.LoyaltyTitle = UserInputSanitizer.TrimAndTruncate(c.Account.LoyaltyTitle, 64);
                c.Account.Position = UserInputSanitizer.TrimAndTruncate(c.Account.Position, 64);
            }

            foreach(AccountBadge badge in c.Badges)
            {
                badge.Name = UserInputSanitizer.TrimAndTruncate(badge.Name, 128);
                badge.Description = UserInputSanitizer.TrimAndTruncate(badge.Description, 256);
                badge.IconUrl = UserInputSanitizer.TrimAndTruncate(badge.IconUrl, 256);
            }

            foreach(Achievement achievement in c.Achievements)
            {
                achievement.Name = UserInputSanitizer.TrimAndTruncate(achievement.Name, 128);
            }

            foreach(House house in c.Houses)
            {
                house.Name = UserInputSanitizer.TrimAndTruncate(house.Name, 128);
                house.Town = UserInputSanitizer.TrimAndTruncate(house.Town, 64);
                house.Paid = UserInputSanitizer.TrimAndTruncate(house.Paid, 64);
            }

            foreach(Death death in c.Deaths)
            {
                death.Reason = UserInputSanitizer.TrimAndTruncate(death.Reason, 256);
            }
        }

        private static void ApplyScalarUpdates(CharacterEntity target, Character src)
        {
            target.Vocation = src.Vocation;
            target.Level = src.Level;
            target.GuildName = src.GuildName;
            target.Residence = src.Residence;
            target.Title = src.Title;
            target.AccountStatus = src.AccountStatus;
            target.AchievementPoints = src.AchievementPoints;
            target.Sex = src.Sex;
            target.LastLogin = src.LastLogin;
        }

        private static void SyncByKey<T, TKey>(ICollection<T> current, IEnumerable<T> incoming, Func<T, TKey> keySelector, Action<T, T> updateAction) where TKey : notnull
        {
            Dictionary<TKey, T> existing = current.ToDictionary(keySelector);
            List<T> incList = incoming?.ToList() ?? [];
            HashSet<TKey> incKeys = new(incList.Select(keySelector));

            foreach(T inc in incList)
            {
                TKey key = keySelector(inc);
                if(existing.TryGetValue(key, out T? cur))
                {
                    updateAction(cur, inc);
                }
                else
                {
                    current.Add(inc);
                }
            }

            List<T> toRemove = current.Where(c => !incKeys.Contains(keySelector(c))).ToList();
            foreach(T rem in toRemove)
            {
                current.Remove(rem);
            }
        }
    }
}
