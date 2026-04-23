using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.Core.Hunts;
using TibiaHuntMaster.Core.Security;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Content;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.Hunts;
using TibiaHuntMaster.Infrastructure.Services.Parsing;
using CreatureEntitys = TibiaHuntMaster.Infrastructure.Data.Entities.Content.CreatureEntity;
using HuntingPlaceEntitys = TibiaHuntMaster.Infrastructure.Data.Entities.Content.HuntingPlaceEntity;

namespace TibiaHuntMaster.Tests.Hunts
{
    public sealed class HuntAnalyticsTests
    {
        private const string realWorldInput = "..."; // (Dein langer String hier, habe ihn gekürzt für Übersicht)

        // ... (Math_XpFormula, Parser_Reads_RealWorld_Data, Parser_Reads_Messy_Death_Input, Parser_Distinguishes_Raw_XP bleiben UNVERÄNDERT) ...

        [Fact(DisplayName = "💾 Service: Imports to DB (Integration)")]
        public async Task Service_Integration()
        {
            SqliteConnection conn = new("DataSource=:memory:");
            conn.Open();
            DbContextOptions<AppDbContext> opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
            TestDbContextFactory factory = new(opts);

            // Setup
            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
                db.Characters.Add(new CharacterEntity
                {
                    Name = "TestChar",
                    World = "Secura",
                    Vocation = "Knight",
                    LastUpdatedUtc = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            // Service mit Factory
            HuntSessionService svc = new(factory, new HuntAnalyzerParser(NullLogger<HuntAnalyzerParser>.Instance), NullLogger<HuntSessionService>.Instance);

            // Import (nutze hier einen simplen String für den Integrationstest, oder deinen realWorldInput)

            SessionImportOptions simpleInput = new(
                CharacterName: "TestChar",
                RawText: "Session data:\nSession: 00:10h\nXP Gain: 225,485\nBalance: 20,662",
                IsDoubleXp: false,
                IsDoubleLoot: false,
                IsRapidRespawn: false,
                Notes: ""
            );

            var import = await svc.ImportSessionAsync(simpleInput);
            import.Result.Should().Be(SessionImportResult.Success, import.Error);

            // Check DB
            HuntStatistics? stats = await svc.GetStatisticsAsync("TestChar");
            stats.Should().NotBeNull();
            stats!.TotalProfit.Should().Be(20_662);

            // Clean up
            conn.Dispose();
        }

        [Fact(DisplayName = "💾 Service: Character lookup is case-insensitive")]
        public async Task Service_Integration_CaseInsensitiveCharacterLookup()
        {
            SqliteConnection conn = new("DataSource=:memory:");
            conn.Open();
            DbContextOptions<AppDbContext> opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
            TestDbContextFactory factory = new(opts);

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
                db.Characters.Add(new CharacterEntity
                {
                    Name = "TestChar",
                    World = "Secura",
                    Vocation = "Knight",
                    LastUpdatedUtc = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            HuntSessionService svc = new(factory, new HuntAnalyzerParser(NullLogger<HuntAnalyzerParser>.Instance), NullLogger<HuntSessionService>.Instance);

            SessionImportOptions input = new(
                CharacterName: "testchar",
                RawText: "Session data:\nSession: 00:10h\nXP Gain: 225,485\nBalance: 20,662",
                IsDoubleXp: false,
                IsDoubleLoot: false,
                IsRapidRespawn: false,
                Notes: ""
            );

            var import = await svc.ImportSessionAsync(input);

            import.Result.Should().Be(SessionImportResult.Success);

            HuntStatistics? stats = await svc.GetStatisticsAsync("TESTCHAR");
            stats.Should().NotBeNull();
            stats!.SessionCount.Should().Be(1);

            conn.Dispose();
        }

        [Fact(DisplayName = "🛡️ Service: Rejects overlong hunt log input")]
        public async Task Service_ShouldReject_OverlongRawInput()
        {
            SqliteConnection conn = new("DataSource=:memory:");
            conn.Open();
            DbContextOptions<AppDbContext> opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
            TestDbContextFactory factory = new(opts);

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
                db.Characters.Add(new CharacterEntity
                {
                    Name = "TestChar",
                    World = "Secura",
                    Vocation = "Knight",
                    LastUpdatedUtc = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            HuntSessionService svc = new(factory, new HuntAnalyzerParser(NullLogger<HuntAnalyzerParser>.Instance), NullLogger<HuntSessionService>.Instance);
            SessionImportOptions input = new(
                CharacterName: "TestChar",
                RawText: new string('X', UserInputLimits.HuntLogMaxLength + 1),
                IsDoubleXp: false,
                IsDoubleLoot: false,
                IsRapidRespawn: false,
                Notes: ""
            );

            var result = await svc.ImportSessionAsync(input);

            result.Result.Should().Be(SessionImportResult.ParseError);
            result.Error.Should().Contain("Input too large");
            conn.Dispose();
        }

        [Fact(DisplayName = "🛡️ Service: Truncates overlong notes before save")]
        public async Task Service_ShouldTruncate_OverlongNotes()
        {
            SqliteConnection conn = new("DataSource=:memory:");
            conn.Open();
            DbContextOptions<AppDbContext> opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
            TestDbContextFactory factory = new(opts);

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
                db.Characters.Add(new CharacterEntity
                {
                    Name = "TestChar",
                    World = "Secura",
                    Vocation = "Knight",
                    LastUpdatedUtc = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            HuntSessionService svc = new(factory, new HuntAnalyzerParser(NullLogger<HuntAnalyzerParser>.Instance), NullLogger<HuntSessionService>.Instance);
            SessionImportOptions input = new(
                CharacterName: "TestChar",
                RawText: "Session data:\nSession: 00:10h\nXP Gain: 225,485\nBalance: 20,662",
                IsDoubleXp: false,
                IsDoubleLoot: false,
                IsRapidRespawn: false,
                Notes: new string('N', UserInputLimits.SessionNotesMaxLength + 120)
            );

            var result = await svc.ImportSessionAsync(input);

            result.Result.Should().Be(SessionImportResult.Success);
            result.Session.Should().NotBeNull();
            result.Session!.Notes.Should().NotBeNull();
            result.Session.Notes!.Length.Should().Be(UserInputLimits.SessionNotesMaxLength);
            conn.Dispose();
        }

        [Fact(DisplayName = "🧭 Service: Infers hunting place when monster match is clear")]
        public async Task Service_ShouldInfer_HuntingPlace_WhenMatchIsClear()
        {
            SqliteConnection conn = new("DataSource=:memory:");
            conn.Open();
            DbContextOptions<AppDbContext> opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
            TestDbContextFactory factory = new(opts);

            int dragonLairId;

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();

                db.Characters.Add(new CharacterEntity
                {
                    Name = "TestChar",
                    World = "Secura",
                    Vocation = "Knight",
                    LastUpdatedUtc = DateTimeOffset.UtcNow
                });

                CreatureEntitys dragon = new() { ContentId = 1, Name = "dragon", ActualName = "Dragon", ContentHash = "dragon", UpdatedAt = DateTimeOffset.UtcNow };
                CreatureEntitys dragonLord = new() { ContentId = 2, Name = "dragon lord", ActualName = "Dragon Lord", ContentHash = "dragon-lord", UpdatedAt = DateTimeOffset.UtcNow };
                CreatureEntitys minotaur = new() { ContentId = 3, Name = "minotaur", ActualName = "Minotaur", ContentHash = "minotaur", UpdatedAt = DateTimeOffset.UtcNow };

                db.Creatures.AddRange(dragon, dragonLord, minotaur);

                HuntingPlaceEntitys dragonLair = new()
                {
                    ContentId = 100,
                    Name = "Dragon Lair",
                    City = "Kazordoon",
                    TemplateType = "HuntingPlace",
                    Vocation = "Knight",
                    ContentHash = "hp-dragon"
                };
                HuntingPlaceEntitys minotaurCamp = new()
                {
                    ContentId = 101,
                    Name = "Minotaur Camp",
                    City = "Thais",
                    TemplateType = "HuntingPlace",
                    Vocation = "Knight",
                    ContentHash = "hp-minotaur"
                };

                db.HuntingPlaces.AddRange(dragonLair, minotaurCamp);
                await db.SaveChangesAsync();

                db.HuntingPlaceCreatures.AddRange(
                    new HuntingPlaceCreatureEntity { HuntingPlaceId = dragonLair.Id, CreatureId = dragon.Id },
                    new HuntingPlaceCreatureEntity { HuntingPlaceId = dragonLair.Id, CreatureId = dragonLord.Id },
                    new HuntingPlaceCreatureEntity { HuntingPlaceId = minotaurCamp.Id, CreatureId = minotaur.Id });

                await db.SaveChangesAsync();
                dragonLairId = dragonLair.Id;
            }

            HuntSessionService svc = new(factory, new HuntAnalyzerParser(NullLogger<HuntAnalyzerParser>.Instance), NullLogger<HuntSessionService>.Instance);
            SessionImportOptions input = new(
                CharacterName: "TestChar",
                RawText:
                """
                Session data:
                Session: 00:20h
                XP Gain: 225,485
                Loot: 295,770
                Supplies: 104,813
                Balance: 190,957
                Killed Monsters:
                6x dragon
                2x dragon lord
                Looted Items:
                2x a platinum coin
                """,
                IsDoubleXp: false,
                IsDoubleLoot: false,
                IsRapidRespawn: false,
                Notes: ""
            );

            var import = await svc.ImportSessionAsync(input);

            import.Result.Should().Be(SessionImportResult.Success);
            import.Session.Should().NotBeNull();
            import.Session!.HuntingPlaceId.Should().Be(dragonLairId);
            conn.Dispose();
        }

        [Fact(DisplayName = "🧭 Service: Leaves hunting place empty when match is ambiguous")]
        public async Task Service_ShouldNotInfer_HuntingPlace_WhenAmbiguous()
        {
            SqliteConnection conn = new("DataSource=:memory:");
            conn.Open();
            DbContextOptions<AppDbContext> opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
            TestDbContextFactory factory = new(opts);

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();

                db.Characters.Add(new CharacterEntity
                {
                    Name = "TestChar",
                    World = "Secura",
                    Vocation = "Knight",
                    LastUpdatedUtc = DateTimeOffset.UtcNow
                });

                CreatureEntitys dragon = new() { ContentId = 1, Name = "dragon", ActualName = "Dragon", ContentHash = "dragon", UpdatedAt = DateTimeOffset.UtcNow };
                db.Creatures.Add(dragon);

                HuntingPlaceEntitys placeA = new()
                {
                    ContentId = 100,
                    Name = "Dragon Cave A",
                    City = "Ankrahmun",
                    TemplateType = "HuntingPlace",
                    Vocation = "Knight",
                    ContentHash = "hp-a"
                };
                HuntingPlaceEntitys placeB = new()
                {
                    ContentId = 101,
                    Name = "Dragon Cave B",
                    City = "Darashia",
                    TemplateType = "HuntingPlace",
                    Vocation = "Knight",
                    ContentHash = "hp-b"
                };

                db.HuntingPlaces.AddRange(placeA, placeB);
                await db.SaveChangesAsync();

                db.HuntingPlaceCreatures.AddRange(
                    new HuntingPlaceCreatureEntity { HuntingPlaceId = placeA.Id, CreatureId = dragon.Id },
                    new HuntingPlaceCreatureEntity { HuntingPlaceId = placeB.Id, CreatureId = dragon.Id });
                await db.SaveChangesAsync();
            }

            HuntSessionService svc = new(factory, new HuntAnalyzerParser(NullLogger<HuntAnalyzerParser>.Instance), NullLogger<HuntSessionService>.Instance);
            SessionImportOptions input = new(
                CharacterName: "TestChar",
                RawText:
                """
                Session data:
                Session: 00:10h
                XP Gain: 125,000
                Balance: 10,000
                Killed Monsters:
                5x dragon
                Looted Items:
                1x a gold coin
                """,
                IsDoubleXp: false,
                IsDoubleLoot: false,
                IsRapidRespawn: false,
                Notes: ""
            );

            var import = await svc.ImportSessionAsync(input);

            import.Result.Should().Be(SessionImportResult.Success);
            import.Session.Should().NotBeNull();
            import.Session!.HuntingPlaceId.Should().BeNull();
            conn.Dispose();
        }

        [Fact(DisplayName = "🌍 Parser: Reads localized (ES) solo hunt labels")]
        public void Parser_ShouldRead_LocalizedSpanishLabels()
        {
            string localizedInput = """
                                    Datos de sesión: Desde 2020-10-06, 20:29:08 hasta 2020-10-06, 20:49:08
                                    Sesión: 00:20h
                                    Ganancia de XP: 225,485
                                    Botín: 295,770
                                    Suministros: 104,813
                                    Balance: 190,957
                                    Monstruos asesinados:
                                    3x dragon
                                    Objetos saqueados:
                                    2x a platinum coin
                                    """;

            HuntAnalyzerParser parser = new(NullLogger<HuntAnalyzerParser>.Instance);
            bool success = parser.TryParse(localizedInput, 1, out var session, out string error);

            success.Should().BeTrue(error);
            session.Should().NotBeNull();
            session!.Duration.TotalMinutes.Should().Be(20);
            session.XpGain.Should().Be(225_485);
            session.Loot.Should().Be(295_770);
            session.Supplies.Should().Be(104_813);
            session.Balance.Should().Be(190_957);
            session.KilledMonsters.Should().ContainSingle(m => m.MonsterName == "dragon" && m.Amount == 3);
            session.LootItems.Should().ContainSingle(i => i.ItemName == "platinum coin" && i.Amount == 2);
        }

        [Fact(DisplayName = "🛡️ Parser: Rejects zero duration even when some values are present")]
        public void Parser_ShouldReject_ZeroDuration()
        {
            string input = """
                           Session data:
                           Session: 00:00h
                           XP Gain: 225,485
                           Balance: 20,662
                           """;

            HuntAnalyzerParser parser = new(NullLogger<HuntAnalyzerParser>.Instance);
            bool success = parser.TryParse(input, 1, out _, out string error);

            success.Should().BeFalse();
            error.Should().Contain("duration is 0");
        }

        [Fact(DisplayName = "🛡️ Parser: Accepts inconsistent loot/supplies/balance combinations for later verification")]
        public void Parser_ShouldAccept_InconsistentBalance()
        {
            string input = """
                           Session data:
                           Session: 00:20h
                           XP Gain: 225,485
                           Loot: 295,770
                           Supplies: 104,813
                           Balance: 999,999
                           """;

            HuntAnalyzerParser parser = new(NullLogger<HuntAnalyzerParser>.Instance);
            bool success = parser.TryParse(input, 1, out HuntSessionEntity? session, out string error);

            success.Should().BeTrue(error);
            session.Should().NotBeNull();
            session!.Balance.Should().Be(999_999);
        }

        [Fact(DisplayName = "🛡️ Parser: Reads raw XP gain when available")]
        public void Parser_ShouldRead_RawXpGain()
        {
            string input = """
                           Session data:
                           Session: 00:20h
                           Raw XP Gain: 1,234,567
                           XP Gain: 2,345,678
                           XP/h: 7,037,034
                           """;

            HuntAnalyzerParser parser = new(NullLogger<HuntAnalyzerParser>.Instance);
            bool success = parser.TryParse(input, 1, out HuntSessionEntity? session, out string error);

            success.Should().BeTrue(error);
            session.Should().NotBeNull();
            session!.RawXpGain.Should().Be(1_234_567);
            session.XpGain.Should().Be(2_345_678);
        }

        [Fact(DisplayName = "🛡️ Parser: Accepts negative XP gain and warns on strong XP/h deviation")]
        public void Parser_ShouldAccept_NegativeXpGain_AndWarnOnXpPerHourDeviation()
        {
            string input = """
                           Session data: From 2026-03-20, 00:09:55 to 2026-03-20, 02:05:50
                             Session: 01:55h
                             XP Gain: -12,031,885
                             XP/h: -144,905,460
                             Loot: 2,002,090
                             Supplies: 153,422
                             Balance: 1,848,668
                           """;

            CapturingLogger<HuntAnalyzerParser> logger = new();
            HuntAnalyzerParser parser = new(logger);

            bool success = parser.TryParse(input, 1, out HuntSessionEntity? session, out string error);

            success.Should().BeTrue(error);
            session.Should().NotBeNull();
            session!.XpGain.Should().Be(-12_031_885);
            session.XpPerHour.Should().Be(-144_905_460);
            session.Balance.Should().Be(1_848_668);
            logger.Entries.Should().Contain(entry =>
                entry.Level == LogLevel.Warning &&
                entry.Message.Contains("Reported XP/h deviates", StringComparison.Ordinal));
        }

        [Fact(DisplayName = "🛡️ Parser: Accepts negative balance when loot and supplies match")]
        public void Parser_ShouldAccept_NegativeBalance_WhenConsistent()
        {
            string input = """
                           Session data:
                           Session: 00:20h
                           XP Gain: 225,485
                           Loot: 10,000
                           Supplies: 30,000
                           Balance: -20,000
                           """;

            HuntAnalyzerParser parser = new(NullLogger<HuntAnalyzerParser>.Instance);
            bool success = parser.TryParse(input, 1, out HuntSessionEntity? session, out string error);

            success.Should().BeTrue(error);
            session.Should().NotBeNull();
            session!.Balance.Should().Be(-20_000);
        }

        [Fact(DisplayName = "🛡️ Parser: Rejects sessions longer than 24 hours")]
        public void Parser_ShouldReject_DurationLongerThan24Hours()
        {
            string input = """
                           Session data:
                           Session: 24:01h
                           XP Gain: 225,485
                           Balance: 20,662
                           """;

            HuntAnalyzerParser parser = new(NullLogger<HuntAnalyzerParser>.Instance);
            bool success = parser.TryParse(input, 1, out _, out string error);

            success.Should().BeFalse();
            error.Should().Contain("exceeds 24h");
        }

        [Fact(DisplayName = "🛡️ Parser: Rejects implausible absolute XP gain")]
        public void Parser_ShouldReject_ImplausibleAbsoluteXpGain()
        {
            string input = """
                           Session data:
                           Session: 00:20h
                           XP Gain: 1,000,000,000,001
                           Balance: 20,662
                           """;

            HuntAnalyzerParser parser = new(NullLogger<HuntAnalyzerParser>.Instance);
            bool success = parser.TryParse(input, 1, out _, out string error);

            success.Should().BeFalse();
            error.Should().Contain("plausible maximum");
        }

        // Helper Factory
        private class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
        {
            public AppDbContext CreateDbContext()
            {
                return new AppDbContext(options);
            }
            public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default)
            {
                return Task.FromResult(new AppDbContext(options));
            }
        }

        private sealed class CapturingLogger<T> : ILogger<T>
        {
            public List<(LogLevel Level, string Message)> Entries { get; } = [];

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Entries.Add((logLevel, formatter(state, exception)));
            }

            private sealed class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new();

                public void Dispose()
                {
                }
            }
        }
    }
}
