using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using FluentAssertions;

using Moq;

using TibiaHuntMaster.App.Services.Diagnostics;
using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Map;
using TibiaHuntMaster.App.Services.Summaries;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Content;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Tests.Services
{
    public sealed class HuntSummaryGeneratorServiceTests
    {
        private int _nextItemContentId = 1;
        private readonly Mock<ILocalizationService> _localizationServiceMock;
        private readonly Mock<IMonsterImageCatalogService> _monsterImageCatalogServiceMock;
        private readonly HuntSummaryGeneratorService _service;

        public HuntSummaryGeneratorServiceTests()
        {
            _localizationServiceMock = new Mock<ILocalizationService>();
            _monsterImageCatalogServiceMock = new Mock<IMonsterImageCatalogService>();
            Dictionary<string, string> translations = new(StringComparer.Ordinal)
            {
                ["Analyzer_SummarySoloTitle"] = "Solo hunt summary",
                ["Analyzer_SummaryTeamTitle"] = "Team hunt summary",
                ["Analyzer_SummarySubtitleSolo"] = "{0} • {1}",
                ["Analyzer_SummarySubtitleTeam"] = "{0} party • {1}",
                ["Analyzer_SummaryModeLine"] = "Mode: {0}",
                ["Analyzer_SummaryModeSolo"] = "Solo",
                ["Analyzer_SummaryModeTeam"] = "Team",
                ["Analyzer_SummaryBaselineLine"] = "Baseline: {0}m normalized from {1}",
                ["Analyzer_SummaryDurationLine"] = "Duration: {0}",
                ["Analyzer_SummaryXpPerHourLine"] = "XP/h: {0}",
                ["Analyzer_SummaryXpGainLine"] = "XP gain: {0}",
                ["Analyzer_SummaryRawXpPerHourLine"] = "Raw XP/h: {0}",
                ["Analyzer_SummaryRawXpPerHourEstimatedLine"] = "Raw XP/h (estimated): {0}",
                ["Analyzer_SummaryRawXpPerHourUnavailableLine"] = "Raw XP/h unavailable",
                ["Analyzer_SummaryBalanceLine"] = "Balance: {0}",
                ["Analyzer_SummaryLootLine"] = "Loot: {0}",
                ["Analyzer_SummarySuppliesLine"] = "Supplies: {0}",
                ["Analyzer_SummaryImbuementLine"] = "Imbuement: {0}",
                ["Analyzer_SummaryPlaceLine"] = "Hunting place: {0}",
                ["Analyzer_SummaryMembersLine"] = "Members: {0}",
                ["Analyzer_SummaryGoldValue"] = "{0} gp",
                ["Analyzer_SummaryGoldPerOzValue"] = "{0} gp/oz",
                ["Analyzer_SummaryRespawnProfileTitle"] = "Respawn profile:",
                ["Analyzer_SummaryRespawnFocusLine"] = "Focus: {0}",
                ["Analyzer_SummaryRespawnFocusXp"] = "XP-leaning",
                ["Analyzer_SummaryRespawnFocusBalanced"] = "Balanced",
                ["Analyzer_SummaryRespawnFocusLoot"] = "Loot-leaning",
                ["Analyzer_SummaryBalancePerHourLine"] = "Net gp/h: {0}",
                ["Analyzer_SummaryTrackedItemValueLine"] = "Tracked item value: {0}",
                ["Analyzer_SummaryHighValueShareLine"] = "High-value share: {0}",
                ["Analyzer_SummaryCommonDropShareLine"] = "Common-drop share: {0}",
                ["Analyzer_SummaryBestDensityItemLine"] = "Best density item: {0} ({1})",
                ["Analyzer_SummaryTopMonstersTitle"] = "Top monsters:",
                ["Analyzer_SummaryMonsterEntry"] = "- {0} x{1}",
                ["Analyzer_SummaryTopItemsTitle"] = "Top items by value/oz:",
                ["Analyzer_SummaryTopItemEntry"] = "- {0} x{1} • {2} • {3}",
                ["Analyzer_SummaryCommonDropsTitle"] = "Common drops (<10k each):",
                ["Analyzer_SummaryCommonDropEntry"] = "- {0} x{1} • {2} total • {3} each",
                ["Analyzer_SummaryMemberBreakdownTitle"] = "Member breakdown:",
                ["Analyzer_SummaryMemberEntry"] = "- {0} | Balance {1} | Damage {2}",
                ["Analyzer_SummaryTransfersTitle"] = "Transfers:",
                ["Analyzer_SummaryTransferEntry"] = "- {0} -> {1}: {2}",
                ["Analyzer_SummaryTransfersOmitted"] = "Transfers omitted in 15m baseline mode."
            };

            _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => translations.TryGetValue(key, out string? value) ? value : key);
            _localizationServiceMock
            .SetupGet(x => x.CurrentCulture)
            .Returns(CultureInfo.InvariantCulture);

            AppDataPaths paths = new(Path.Combine(Path.GetTempPath(), "thm-summary-tests", Guid.NewGuid().ToString("N")));
            paths.EnsureDirectories();
            _monsterImageCatalogServiceMock
            .Setup(x => x.TryResolveImageUri(It.IsAny<int?>(), It.IsAny<string?>(), out It.Ref<string>.IsAny))
            .Returns(false);
            _service = new HuntSummaryGeneratorService(paths, _localizationServiceMock.Object, _monsterImageCatalogServiceMock.Object);
        }

        [Fact]
        public void BuildText_ShouldWrapDiscordAndIncludeDetailedSoloSections()
        {
            HuntSessionEntity session = CreateSoloSession();
            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Discord,
                HuntSummaryTemplatePreset.Detailed,
                false,
                true,
                session,
                null,
                [],
                "Falcon Bastion",
                4_000_000,
                400_000,
                300_000,
                40_000,
                2_400_000);

            string summary = _service.BuildText(request);

            summary.Should().StartWith("```ansi");
            summary.Should().Contain("Solo hunt summary");
            summary.Should().Contain("Hunting place:");
            summary.Should().Contain("Falcon Bastion");
            summary.Should().Contain("Raw XP/h");
            summary.Should().Contain("Respawn profile:");
            summary.Should().Contain("Top monsters:");
            summary.Should().Contain("- Grim Reaper x12");
            summary.Should().Contain("Balance:");
            summary.Should().Contain("\u001b[1;32m400,000 gp\u001b[0m");
            summary.Should().EndWith("```", because: "Discord output must stay ready to paste");
        }

        [Fact]
        public void BuildPreviewText_ShouldScaleTotalsToQuarterHourBaseline()
        {
            HuntSessionEntity session = CreateSoloSession();
            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Text,
                HuntSummaryTemplatePreset.Compact,
                true,
                true,
                session,
                null,
                [],
                null,
                4_000_000,
                400_000,
                300_000,
                0,
                2_400_000);

            string summary = _service.BuildPreviewText(request);

            summary.Should().Contain("Baseline: 15m normalized from 00:30");
            summary.Should().Contain("XP gain: 1,000,000");
            summary.Should().Contain("Raw XP/h: 4,800,000");
            summary.Should().Contain("Balance: 200,000 gp");
            summary.Should().Contain("Loot: 400,000 gp");
            summary.Should().Contain("Supplies: 150,000 gp");
        }

        [Fact]
        public void BuildPreviewText_ShouldRespectPresetAndOmitMonsterSectionInCompactMode()
        {
            HuntSessionEntity session = CreateSoloSession();
            HuntSummaryRequest compactRequest = new(
                "Tentakel",
                HuntSummaryFormat.Text,
                HuntSummaryTemplatePreset.Compact,
                false,
                true,
                session,
                null,
                [],
                null,
                4_000_000,
                400_000,
                300_000,
                0,
                2_400_000);

            HuntSummaryRequest detailedRequest = compactRequest with { Preset = HuntSummaryTemplatePreset.Detailed };

            string compact = _service.BuildPreviewText(compactRequest);
            string detailed = _service.BuildPreviewText(detailedRequest);

            compact.Should().NotContain("Top monsters:");
            detailed.Should().Contain("Top monsters:");
        }

        [Fact]
        public void BuildPreviewText_ShouldUseEffectiveDurationForRawXpPerHourAndBaseline()
        {
            HuntSessionEntity session = CreateSoloSession();
            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Text,
                HuntSummaryTemplatePreset.Compact,
                true,
                true,
                session,
                null,
                [],
                null,
                4_800_000,
                400_000,
                300_000,
                0,
                2_400_000,
                TimeSpan.FromMinutes(25));

            string summary = _service.BuildPreviewText(request);

            summary.Should().Contain("Baseline: 15m normalized from 00:25");
            summary.Should().Contain("Raw XP/h: 5,760,000");
        }

        [Fact]
        public void BuildText_ShouldOmitTransfersWhenNormalizedTeamSummaryIsDetailed()
        {
            TeamHuntSessionEntity session = CreateTeamSession();
            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Text,
                HuntSummaryTemplatePreset.Detailed,
                true,
                true,
                null,
                session,
                [new HuntSummaryTransfer("Tentakel", "Knight", 250_000)],
                null,
                5_500_000,
                900_000,
                600_000,
                0,
                18_000_000);

            string summary = _service.BuildText(request);

            summary.Should().Contain("Transfers omitted in 15m baseline mode.");
            summary.Should().NotContain("Tentakel -> Knight");
        }

        [Fact]
        public void BuildText_ShouldIncludeTransfersForRawDetailedTeamSummary()
        {
            TeamHuntSessionEntity session = CreateTeamSession();
            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Text,
                HuntSummaryTemplatePreset.Detailed,
                false,
                true,
                null,
                session,
                [new HuntSummaryTransfer("Tentakel", "Knight", 250_000)],
                null,
                5_500_000,
                900_000,
                600_000,
                0,
                18_000_000);

            string summary = _service.BuildText(request);

            summary.Should().Contain("Transfers:");
            summary.Should().Contain("Tentakel -> Knight: 250,000 gp");
        }

        [Fact]
        public void BuildText_ShouldEstimateSoloRawXpPerHourWithoutExplicitRawValue()
        {
            HuntSessionEntity session = CreateSoloSession();
            session.RawInput = string.Empty;
            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Discord,
                HuntSummaryTemplatePreset.Compact,
                false,
                true,
                session,
                null,
                [],
                null,
                4_000_000,
                -125_000,
                925_000,
                0,
                null);

            string summary = _service.BuildText(request);

            summary.Should().Contain("Raw XP/h (estimated)");
            summary.Should().Contain("2,666,667");
            summary.Should().Contain("Balance:");
            summary.Should().Contain("\u001b[1;31m-125,000 gp\u001b[0m");
            summary.Should().Contain("Loot:");
            summary.Should().Contain("\u001b[1;33m800,000 gp\u001b[0m");
            summary.Should().Contain("XP/h:");
            summary.Should().Contain("\u001b[1;36m4,000,000\u001b[0m");
        }

        [Fact]
        public void BuildText_ShouldEstimateSoloRawXpPerHourFromDoubleXpWhenRawXpIsMissing()
        {
            HuntSessionEntity session = CreateSoloSession();
            session.IsDoubleXp = true;
            session.RawInput = string.Empty;

            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Text,
                HuntSummaryTemplatePreset.Compact,
                false,
                true,
                session,
                null,
                [],
                null,
                6_000_000,
                400_000,
                300_000,
                0,
                null);

            string summary = _service.BuildText(request);

            summary.Should().Contain("Raw XP/h (estimated): 2,000,000");
        }

        [Fact]
        public void BuildText_ShouldEstimateSoloRawXpPerHourUsingBoostMinutes()
        {
            HuntSessionEntity session = CreateSoloSession();
            session.RawInput = string.Empty;

            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Text,
                HuntSummaryTemplatePreset.Compact,
                false,
                true,
                session,
                null,
                [],
                null,
                3_500_000,
                400_000,
                300_000,
                0,
                null,
                TimeSpan.FromMinutes(30),
                50,
                15,
                null);

            string summary = _service.BuildText(request);

            summary.Should().Contain("Raw XP/h (estimated): 2,000,000");
        }

        [Fact]
        public void BuildText_ShouldEstimateSoloRawXpPerHourUsingCustomRateOverride()
        {
            HuntSessionEntity session = CreateSoloSession();
            session.RawInput = string.Empty;
            session.IsDoubleXp = true;

            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Text,
                HuntSummaryTemplatePreset.Compact,
                false,
                true,
                session,
                null,
                [],
                null,
                5_000_000,
                400_000,
                300_000,
                0,
                null,
                TimeSpan.FromMinutes(30),
                null,
                null,
                125);

            string summary = _service.BuildText(request);

            summary.Should().Contain("Raw XP/h (estimated): 2,000,000");
        }

        [Fact]
        public void BuildText_ShouldMarkTeamRawXpUnavailableWhenRawLogValueIsMissing()
        {
            TeamHuntSessionEntity session = CreateTeamSession();
            session.RawInput = string.Empty;

            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Text,
                HuntSummaryTemplatePreset.Compact,
                false,
                true,
                null,
                session,
                [],
                null,
                5_500_000,
                900_000,
                600_000,
                0,
                null);

            string summary = _service.BuildText(request);

            summary.Should().Contain("Raw XP/h unavailable");
        }

        [Fact]
        public void BuildText_ShouldHideRawXpLines_WhenDisabled()
        {
            HuntSessionEntity session = CreateSoloSession();
            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Text,
                HuntSummaryTemplatePreset.Compact,
                false,
                false,
                session,
                null,
                [],
                null,
                4_000_000,
                400_000,
                300_000,
                0,
                2_400_000);

            string summary = _service.BuildText(request);

            summary.Should().NotContain("Raw XP/h");
        }

        [Fact]
        public async Task BuildText_ShouldIncludeDetailedLootInsights_WhenItemMetadataExists()
        {
            await using TestDatabaseContext testDb = await TestDatabaseContext.CreateAsync();
            testDb.DbContext.Items.AddRange(
                CreateItem("grimeleech wing", 13_000, 8.2m),
                CreateItem("great health potion", 190, 2.2m),
                CreateItem("silver token", 250, 0.1m));
            await testDb.DbContext.SaveChangesAsync();

            AppDataPaths paths = new(Path.Combine(Path.GetTempPath(), "thm-summary-tests", Guid.NewGuid().ToString("N")));
            paths.EnsureDirectories();
            HuntSummaryGeneratorService service = new(
                paths,
                _localizationServiceMock.Object,
                _monsterImageCatalogServiceMock.Object,
                testDb.Factory);

            HuntSessionEntity session = CreateSoloSession();
            session.LootItems =
            [
                new HuntLootEntry { ItemName = "grimeleech wing", Amount = 4 },
                new HuntLootEntry { ItemName = "great health potion", Amount = 17 },
                new HuntLootEntry { ItemName = "silver token", Amount = 9 }
            ];

            HuntSummaryRequest request = new(
                "Tentakel",
                HuntSummaryFormat.Text,
                HuntSummaryTemplatePreset.Detailed,
                true,
                true,
                session,
                null,
                [],
                null,
                4_000_000,
                400_000,
                300_000,
                0,
                2_400_000);

            string summary = service.BuildText(request);

            summary.Should().Contain("Respawn profile:");
            summary.Should().Contain("Tracked item value:");
            summary.Should().Contain("Best density item:");
            summary.Should().Contain("Top items by value/oz:");
            summary.Should().Contain("grimeleech wing");
            summary.Should().Contain("gp/oz");
            summary.Should().Contain("Common drops (<10k each):");
            summary.Should().Contain("great health potion");
        }

        [Fact]
        public void TryExtractRawXpGain_ShouldParseRawXpGainFromLog()
        {
            long? result = HuntSummaryGeneratorService.TryExtractRawXpGain("Session data:\nRaw XP Gain: 4,321,000\nXP Gain: 3,210,000");

            result.Should().Be(4_321_000);
        }

        private static HuntSessionEntity CreateSoloSession()
        {
            return new HuntSessionEntity
            {
                SessionStartTime = new DateTimeOffset(2026, 3, 26, 18, 30, 0, TimeSpan.Zero),
                ImportedAt = new DateTimeOffset(2026, 3, 26, 19, 0, 0, TimeSpan.Zero),
                Duration = TimeSpan.FromMinutes(30),
                XpGain = 2_000_000,
                RawInput = "Raw XP Gain: 2,400,000",
                Loot = 800_000,
                Supplies = 250_000,
                LootItems = [],
                KilledMonsters =
                [
                    new HuntMonsterEntry { MonsterName = "Grim Reaper", Amount = 12 },
                    new HuntMonsterEntry { MonsterName = "Hellhound", Amount = 7 }
                ]
            };
        }

        private static TeamHuntSessionEntity CreateTeamSession()
        {
            return new TeamHuntSessionEntity
            {
                SessionStartTime = new DateTimeOffset(2026, 3, 26, 20, 0, 0, TimeSpan.Zero),
                ImportedAt = new DateTimeOffset(2026, 3, 26, 21, 0, 0, TimeSpan.Zero),
                Duration = TimeSpan.FromHours(1),
                XpGain = 12_000_000,
                RawInput = "Raw XP Gain: 18,000,000",
                XpPerHour = 5_500_000,
                TotalBalance = 900_000,
                TotalLoot = 1_500_000,
                TotalSupplies = 600_000,
                Members =
                [
                    new TeamHuntMemberEntity { Name = "Tentakel", Balance = 300_000, Damage = 4_200_000 },
                    new TeamHuntMemberEntity { Name = "Knight", Balance = 600_000, Damage = 5_100_000 }
                ]
            };
        }

        private ItemEntity CreateItem(string name, long value, decimal weightOz)
        {
            return new ItemEntity
            {
                ContentId = _nextItemContentId++,
                Name = name,
                ActualName = name,
                NormalizedName = name.Trim().ToUpperInvariant(),
                Value = value,
                WeightOz = weightOz
            };
        }

        private sealed class TestDatabaseContext : IAsyncDisposable
        {
            private readonly SqliteConnection _connection;

            private TestDatabaseContext(SqliteConnection connection, AppDbContext dbContext, IDbContextFactory<AppDbContext> factory)
            {
                _connection = connection;
                DbContext = dbContext;
                Factory = factory;
            }

            public AppDbContext DbContext { get; }

            public IDbContextFactory<AppDbContext> Factory { get; }

            public static async Task<TestDatabaseContext> CreateAsync()
            {
                SqliteConnection connection = new("DataSource=:memory:");
                await connection.OpenAsync();

                DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                         .UseSqlite(connection)
                                                         .Options;

                AppDbContext dbContext = new(options);
                await dbContext.Database.EnsureCreatedAsync();

                return new TestDatabaseContext(connection, dbContext, new TestDbContextFactory(connection));
            }

            public async ValueTask DisposeAsync()
            {
                await DbContext.DisposeAsync();
                await _connection.DisposeAsync();
            }
        }

        private sealed class TestDbContextFactory(SqliteConnection connection) : IDbContextFactory<AppDbContext>
        {
            public AppDbContext CreateDbContext()
            {
                DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                         .UseSqlite(connection)
                                                         .Options;

                return new AppDbContext(options);
            }

            public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CreateDbContext());
            }
        }
    }
}
