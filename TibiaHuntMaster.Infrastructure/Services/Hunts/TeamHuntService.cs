using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Security;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.Parsing;

namespace TibiaHuntMaster.Infrastructure.Services.Hunts
{
    public sealed class TeamHuntService(
        IDbContextFactory<AppDbContext> dbFactory,
        TeamHuntParser parser,
        ILogger<TeamHuntService> logger) : ITeamHuntService
    {
        public async Task<(SessionImportResult Result, TeamHuntSessionEntity? Session, string? Error)> ImportTeamSessionAsync(string rawText, string characterName, CancellationToken ct = default)
        {
            logger.LogInformation("Importing team hunt session for character: {CharacterName}", characterName);

            if(UserInputSanitizer.ExceedsLength(rawText, UserInputLimits.HuntLogMaxLength))
            {
                return (SessionImportResult.ParseError, null, $"Input too large (max {UserInputLimits.HuntLogMaxLength} characters).");
            }

            string safeRawText = UserInputSanitizer.Truncate(rawText, UserInputLimits.HuntLogMaxLength);
            return await SqliteWriteRetry.ExecuteAsync<(SessionImportResult Result, TeamHuntSessionEntity? Session, string? Error)>(
                async token =>
                {
                    await using AppDbContext db = await dbFactory.CreateDbContextAsync(token);

                    CharacterEntity? character = await ResolveCharacterByNameAsync(db, characterName, token);
                    if (character == null)
                    {
                        logger.LogWarning("Character not found: {CharacterName}", characterName);
                        return (SessionImportResult.CharacterNotFound, null, "Character not found.");
                    }

                    if (!parser.TryParse(safeRawText, character.Id, out TeamHuntSessionEntity? session, out string parseError))
                    {
                        logger.LogWarning("Failed to parse team hunt session: {Error}", parseError);
                        return (SessionImportResult.ParseError, null, parseError);
                    }

                    if (session == null)
                    {
                        logger.LogError("Session is unexpectedly null after parsing");
                        return (SessionImportResult.ParseError, null, "Internal error: session is null");
                    }

                    NormalizeTeamSessionForPersistence(session);
                    if (session.Members.Count == 0)
                    {
                        return (SessionImportResult.ParseError, null, "No party members found.");
                    }

                    await using var transaction = await db.Database.BeginTransactionAsync(token);

                    bool exists = await db.TeamHuntSessions.AnyAsync(s =>
                        s.CharacterId == character.Id &&
                        s.SessionStartTime == session.SessionStartTime &&
                        s.TotalBalance == session.TotalBalance,
                        token);

                    if (exists)
                    {
                        logger.LogInformation("Team hunt session already exists (duplicate), skipping import");
                        return (SessionImportResult.Duplicate, null, "Team session already exists.");
                    }

                    db.TeamHuntSessions.Add(session);
                    await db.SaveChangesAsync(token);
                    await transaction.CommitAsync(token);

                    logger.LogInformation("Team hunt session imported successfully. SessionId: {SessionId}", session.Id);
                    return (SessionImportResult.Success, session, null);
                },
                logger,
                "team hunt import",
                ct);
        }

        /// <summary>
        ///     Versucht, einen bestehenden Team-Hunt zu finden, der zeitlich zum XP-Log passt, und fügt XP-Daten hinzu.
        /// </summary>
        public async Task<TeamHuntSessionEntity?> EnrichLatestSessionIfMatching(string charName, DateTimeOffset startTime, long xpGain, long xpPerHour)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();

            CharacterEntity? charEntity = await ResolveCharacterByNameAsync(db, charName);
            if(charEntity == null)
            {
                return null;
            }

            // Zeitfenster: +/- 10 Minuten Toleranz, da Logs oft leicht versetzt kopiert werden
            DateTimeOffset min = startTime.AddMinutes(-10);
            DateTimeOffset max = startTime.AddMinutes(10);

            // Wir suchen einen Team Hunt:
            // 1. Vom selben Char
            // 2. Der noch keine XP hat (XpGain == 0)
            // 3. Der zeitlich passt
            TeamHuntSessionEntity? session = await db.TeamHuntSessions
                                                     .Include(t => t.Members) // Wichtig für die Anzeige nach dem Reload
                                                     .Where(t => t.CharacterId == charEntity.Id)
                                                     .Where(t => t.XpGain == 0)
                                                     .Where(t => t.SessionStartTime >= min && t.SessionStartTime <= max)
                                                     .OrderByDescending(t => t.ImportedAt)
                                                     .FirstOrDefaultAsync();

            if(session != null)
            {
                // Update durchführen
                session.XpGain = xpGain;
                session.XpPerHour = xpPerHour;

                db.TeamHuntSessions.Update(session);
                await db.SaveChangesAsync();

                return session;
            }

            return null;
        }
        public async Task UpdateSessionAsync(TeamHuntSessionEntity currentTeamSession)
        {
            NormalizeTeamSessionForPersistence(currentTeamSession);
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();
            db.TeamHuntSessions.Update(currentTeamSession);
            await db.SaveChangesAsync();
        }

        private static async Task<CharacterEntity?> ResolveCharacterByNameAsync(AppDbContext db, string characterName, CancellationToken ct = default)
        {
            string requestedName = UserInputSanitizer.TrimAndTruncate(characterName, UserInputLimits.CharacterNameMaxLength);
            if(string.IsNullOrWhiteSpace(requestedName))
            {
                return null;
            }

            CharacterEntity? exactMatch = await db.Characters
                                                  .FirstOrDefaultAsync(c => c.Name == requestedName, ct);
            if(exactMatch is not null)
            {
                return exactMatch;
            }

            List<CharacterEntity> allCharacters = await db.Characters.ToListAsync(ct);
            return allCharacters.FirstOrDefault(c =>
                string.Equals(c.Name, requestedName, StringComparison.OrdinalIgnoreCase));
        }

        private static void NormalizeTeamSessionForPersistence(TeamHuntSessionEntity session)
        {
            session.RawInput = UserInputSanitizer.Truncate(session.RawInput, UserInputLimits.HuntLogMaxLength);
            session.Notes = UserInputSanitizer.TrimAndTruncateOrNull(session.Notes, UserInputLimits.SessionNotesMaxLength);
            session.LootType = UserInputSanitizer.TrimAndTruncate(session.LootType, UserInputLimits.LootTypeMaxLength);

            for(int i = session.Members.Count - 1; i >= 0; i--)
            {
                TeamHuntMemberEntity member = session.Members[i];
                member.Name = UserInputSanitizer.TrimAndTruncate(member.Name, UserInputLimits.TeamMemberNameMaxLength);
                if(string.IsNullOrWhiteSpace(member.Name))
                {
                    session.Members.RemoveAt(i);
                }
            }
        }
    }
}
