using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.App.ViewModels;
using TibiaHuntMaster.App.ViewModels.Dashboard;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Character;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.Hunts;

namespace TibiaHuntMaster.Tests.ViewModels
{
    /// <summary>
    /// Regression tests for HistoryViewModel to ensure cross-platform reliability,
    /// especially for Windows-specific character name matching issues.
    /// </summary>
    public sealed class HistoryViewModelTests
    {
        /// <summary>
        /// Helper to create a test database context with services
        /// </summary>
        private sealed class TestDatabaseContext : IDisposable
        {
            public AppDbContext DbContext { get; }
            public IDbContextFactory<AppDbContext> DbFactory { get; }
            public HuntMergerService MergerService { get; }
            public HuntGroupingService GroupingService { get; }
            public INavigationService NavigationService { get; }
            public ILocalizationService LocalizationService { get; }
            public ILogger<HistoryViewModel> Logger { get; }

            private readonly System.Data.Common.DbConnection _connection;

            public TestDatabaseContext()
            {
                // Create unique in-memory database for this test
                DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                         .UseSqlite("DataSource=:memory:")
                                                         .Options;

                DbContext = new AppDbContext(options);
                _connection = DbContext.Database.GetDbConnection();
                _connection.Open();
                DbContext.Database.EnsureCreated();

                // Create factory that reuses the same connection
                DbFactory = new TestDbContextFactory(_connection);

                // Create service dependencies
                MergerService = new HuntMergerService();
                GroupingService = new HuntGroupingService(DbFactory);
                NavigationService = new MockNavigationService();
                LocalizationService = new MockLocalizationService();
                Logger = new MockLogger();
            }

            public void Dispose()
            {
                DbContext.Dispose();
                _connection.Dispose();
            }
        }

        /// <summary>
        /// Regression test for Windows history visibility bug:
        /// Ensures history loads correctly when character name differs only in casing.
        ///
        /// Context: Windows file systems are case-insensitive by default, which can lead to
        /// character name matching issues if database collation differs from in-memory comparison.
        /// </summary>
        [Fact]
        public async Task LoadHistoryAsync_ShouldLoadHunts_WhenCharacterCasingDiffers()
        {
            // Arrange
            using TestDatabaseContext testDb = new();

            CharacterEntity character = new()
            {
                Name = "TestCharacter",
                World = "Antica",
                Vocation = "Elite Knight",
                Level = 100
            };
            testDb.DbContext.Characters.Add(character);
            await testDb.DbContext.SaveChangesAsync();

            HuntSessionEntity hunt = new()
            {
                CharacterId = character.Id,
                SessionStartTime = DateTimeOffset.UtcNow.AddHours(-1),
                ImportedAt = DateTimeOffset.UtcNow,
                Loot = 50000,
                Supplies = 10000,
                Balance = 40000,
                XpGain = 500000,
                Duration = TimeSpan.FromMinutes(60)
            };
            testDb.DbContext.HuntSessions.Add(hunt);
            await testDb.DbContext.SaveChangesAsync();

            HistoryViewModel viewModel = new(
                testDb.DbFactory,
                testDb.MergerService,
                testDb.GroupingService,
                testDb.NavigationService,
                testDb.LocalizationService,
                testDb.Logger
            );

            // Verify data was written
            int charCount = testDb.DbContext.Characters.Count();
            int huntCount = testDb.DbContext.HuntSessions.Count();

            // Act: Load history with different casing (lowercase)
            await viewModel.LoadHistoryAsync("testcharacter");

            // Assert: Should find the character and load the hunt
            viewModel.Items.Should().NotBeEmpty($"Expected items (char count: {charCount}, hunt count: {huntCount}, status: {viewModel.StatusMessage})");
            viewModel.Items.Should().HaveCount(1);
            viewModel.Items[0].Session.Should().NotBeNull();
            viewModel.Items[0].Session!.Id.Should().Be(hunt.Id);
            viewModel.StatusMessage.Should().Be("History_EntriesLoaded");
        }

        /// <summary>
        /// Regression test for Windows history visibility bug:
        /// Ensures history loads correctly with non-ASCII character names (e.g., Polish, Spanish, Swedish).
        ///
        /// Context: Unicode handling can differ between platforms, especially when combined with
        /// case-insensitive matching. This test ensures names like "Łukasz" work correctly.
        /// </summary>
        [Fact]
        public async Task LoadHistoryAsync_ShouldLoadHunts_WithNonAsciiCharacterNames()
        {
            // Arrange: Create characters with non-ASCII names from different languages
            CharacterEntity[] characters =
            [
                new CharacterEntity { Name = "Łukasz", World = "Antica", Vocation = "Elder Druid", Level = 150 }, // Polish
                new CharacterEntity { Name = "José", World = "Antica", Vocation = "Royal Paladin", Level = 200 }, // Spanish
                new CharacterEntity { Name = "Björn", World = "Antica", Vocation = "Elite Knight", Level = 180 }, // Swedish
                new CharacterEntity { Name = "Müller", World = "Antica", Vocation = "Master Sorcerer", Level = 220 } // German
            ];

            foreach (CharacterEntity character in characters)
            {
                using TestDatabaseContext testDb = new();

                testDb.DbContext.Characters.Add(character);
                await testDb.DbContext.SaveChangesAsync();

                HuntSessionEntity hunt = new()
                {
                    CharacterId = character.Id,
                    SessionStartTime = DateTimeOffset.UtcNow.AddHours(-1),
                    ImportedAt = DateTimeOffset.UtcNow,
                    Loot = 75000,
                    Supplies = 15000,
                    Balance = 60000,
                    XpGain = 800000,
                    Duration = TimeSpan.FromMinutes(90)
                };
                testDb.DbContext.HuntSessions.Add(hunt);
                await testDb.DbContext.SaveChangesAsync();

                HistoryViewModel viewModel = new(
                    testDb.DbFactory,
                    testDb.MergerService,
                    testDb.GroupingService,
                    testDb.NavigationService,
                    testDb.LocalizationService,
                    testDb.Logger
                );

                // Act: Load history with exact name
                await viewModel.LoadHistoryAsync(character.Name);

                // Assert: Should find the character and load the hunt
                viewModel.Items.Should().NotBeEmpty($"character '{character.Name}' should have history");
                viewModel.Items.Should().HaveCount(1);
                viewModel.Items[0].Session.Should().NotBeNull();
                viewModel.Items[0].Session!.CharacterId.Should().Be(character.Id);
                viewModel.StatusMessage.Should().Be("History_EntriesLoaded");
            }
        }

        /// <summary>
        /// Regression test: Ensures clear error message when character is not found.
        /// </summary>
        [Fact]
        public async Task LoadHistoryAsync_ShouldShowErrorMessage_WhenCharacterNotFound()
        {
            // Arrange
            using TestDatabaseContext testDb = new();

            HistoryViewModel viewModel = new(
                testDb.DbFactory,
                testDb.MergerService,
                testDb.GroupingService,
                testDb.NavigationService,
                testDb.LocalizationService,
                testDb.Logger
            );

            // Act: Try to load history for non-existent character
            await viewModel.LoadHistoryAsync("NonExistentCharacter");

            // Assert: Should show clear error message
            viewModel.Items.Should().BeEmpty();
            viewModel.StatusMessage.Should().Contain("not found");
            viewModel.StatusMessage.Should().Contain("NonExistentCharacter");
        }

        /// <summary>
        /// Regression test: Ensures empty list shows appropriate message, not silent failure.
        /// </summary>
        [Fact]
        public async Task LoadHistoryAsync_ShouldLoadEmptyList_WhenCharacterExistsButNoHunts()
        {
            // Arrange
            using TestDatabaseContext testDb = new();

            CharacterEntity character = new()
            {
                Name = "EmptyHistoryChar",
                World = "Antica",
                Vocation = "Elder Druid",
                Level = 50
            };
            testDb.DbContext.Characters.Add(character);
            await testDb.DbContext.SaveChangesAsync();

            HistoryViewModel viewModel = new(
                testDb.DbFactory,
                testDb.MergerService,
                testDb.GroupingService,
                testDb.NavigationService,
                testDb.LocalizationService,
                testDb.Logger
            );

            // Act
            await viewModel.LoadHistoryAsync(character.Name);

            // Assert: Should have empty list but valid state (not error)
            viewModel.Items.Should().BeEmpty();
            viewModel.StatusMessage.Should().Be("History_StatusEmpty");
        }

        /// <summary>
        /// Regression test: If data exists but active filter hides all entries,
        /// the UI must show an explicit filtered-empty status instead of a silent empty list.
        /// </summary>
        [Fact]
        public async Task LoadHistoryAsync_ShouldShowFilteredStatus_WhenDataExistsButCurrentFilterHidesEntries()
        {
            // Arrange
            using TestDatabaseContext testDb = new();

            CharacterEntity character = new()
            {
                Name = "FilterOnlyTeamChar",
                World = "Antica",
                Vocation = "Royal Paladin",
                Level = 300
            };
            testDb.DbContext.Characters.Add(character);
            await testDb.DbContext.SaveChangesAsync();

            TeamHuntSessionEntity teamHunt = new()
            {
                CharacterId = character.Id,
                SessionStartTime = DateTimeOffset.UtcNow.AddHours(-2),
                ImportedAt = DateTimeOffset.UtcNow,
                Duration = TimeSpan.FromMinutes(45),
                TotalLoot = 250000,
                TotalSupplies = 50000,
                TotalBalance = 200000,
                XpGain = 600000
            };
            testDb.DbContext.TeamHuntSessions.Add(teamHunt);
            await testDb.DbContext.SaveChangesAsync();

            HistoryViewModel viewModel = new(
                testDb.DbFactory,
                testDb.MergerService,
                testDb.GroupingService,
                testDb.NavigationService,
                testDb.LocalizationService,
                testDb.Logger
            )
            {
                // Solo-only filter while only team data exists.
                SelectedTypeFilterIndex = 1
            };

            // Act
            await viewModel.LoadHistoryAsync(character.Name);

            // Assert
            viewModel.Items.Should().BeEmpty();
            viewModel.StatusMessage.Should().Be("History_StatusFilteredEmpty");
        }

        [Fact]
        public async Task LoadHistoryAsync_ShouldPaginateHistoryEntries_InPagesOfTen()
        {
            using TestDatabaseContext testDb = new();

            CharacterEntity character = new()
            {
                Name = "PagedHistoryChar",
                World = "Antica",
                Vocation = "Master Sorcerer",
                Level = 420
            };
            testDb.DbContext.Characters.Add(character);
            await testDb.DbContext.SaveChangesAsync();

            for (int i = 0; i < 15; i++)
            {
                DateTimeOffset sessionDate = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero).AddDays(i);
                testDb.DbContext.HuntSessions.Add(new HuntSessionEntity
                {
                    CharacterId = character.Id,
                    SessionStartTime = sessionDate,
                    ImportedAt = sessionDate,
                    Loot = 100000 + i,
                    Supplies = 25000,
                    Balance = 75000 + i,
                    XpGain = 500000 + i,
                    Duration = TimeSpan.FromMinutes(45)
                });
            }

            await testDb.DbContext.SaveChangesAsync();

            HistoryViewModel viewModel = new(
                testDb.DbFactory,
                testDb.MergerService,
                testDb.GroupingService,
                testDb.NavigationService,
                testDb.LocalizationService,
                testDb.Logger
            );

            await viewModel.LoadHistoryAsync(character.Name);

            viewModel.TotalFilteredItems.Should().Be(15);
            viewModel.TotalPages.Should().Be(2);
            viewModel.CurrentPage.Should().Be(1);
            viewModel.Items.Should().HaveCount(10);
            viewModel.CanGoToNextPage.Should().BeTrue();

            viewModel.NextPageCommand.Execute(null);

            viewModel.CurrentPage.Should().Be(2);
            viewModel.Items.Should().HaveCount(5);
            viewModel.CanGoToPreviousPage.Should().BeTrue();
            viewModel.CanGoToNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task LoadHistoryAsync_ShouldFilterHistoryEntries_ByDateRange()
        {
            using TestDatabaseContext testDb = new();

            CharacterEntity character = new()
            {
                Name = "DateFilterChar",
                World = "Antica",
                Vocation = "Royal Paladin",
                Level = 350
            };
            testDb.DbContext.Characters.Add(character);
            await testDb.DbContext.SaveChangesAsync();

            DateTimeOffset[] huntDates =
            [
                new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero)
            ];

            foreach (DateTimeOffset huntDate in huntDates)
            {
                testDb.DbContext.HuntSessions.Add(new HuntSessionEntity
                {
                    CharacterId = character.Id,
                    SessionStartTime = huntDate,
                    ImportedAt = huntDate,
                    Loot = 80000,
                    Supplies = 20000,
                    Balance = 60000,
                    XpGain = 450000,
                    Duration = TimeSpan.FromMinutes(50)
                });
            }

            await testDb.DbContext.SaveChangesAsync();

            HistoryViewModel viewModel = new(
                testDb.DbFactory,
                testDb.MergerService,
                testDb.GroupingService,
                testDb.NavigationService,
                testDb.LocalizationService,
                testDb.Logger
            )
            {
                FromDateFilter = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero),
                ToDateFilter = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero)
            };

            await viewModel.LoadHistoryAsync(character.Name);

            viewModel.TotalFilteredItems.Should().Be(1);
            viewModel.Items.Should().HaveCount(1);
            viewModel.Items[0].Session.Should().NotBeNull();
            viewModel.Items[0].Session!.SessionStartTime.Date.Should().Be(new DateTime(2026, 1, 5));
        }

        [Fact]
        public async Task LoadHistoryAsync_ShouldKeepInitialResult_WhenGoalFilterIsInitializedDuringFirstLoad()
        {
            using TestDatabaseContext testDb = new();

            CharacterEntity character = new()
            {
                Name = "GoalInitChar",
                World = "Antica",
                Vocation = "Elder Druid",
                Level = 500
            };
            testDb.DbContext.Characters.Add(character);
            await testDb.DbContext.SaveChangesAsync();

            testDb.DbContext.CharacterGoals.Add(new CharacterGoalEntity
            {
                CharacterId = character.Id,
                Title = "Reach 550",
                Type = GoalType.Level,
                StartValue = 500,
                TargetValue = 550,
                IsActive = true
            });

            testDb.DbContext.HuntSessions.Add(new HuntSessionEntity
            {
                CharacterId = character.Id,
                SessionStartTime = DateTimeOffset.UtcNow.AddHours(-1),
                ImportedAt = DateTimeOffset.UtcNow,
                Loot = 120000,
                Supplies = 30000,
                Balance = 90000,
                XpGain = 700000,
                Duration = TimeSpan.FromMinutes(60)
            });
            await testDb.DbContext.SaveChangesAsync();

            HistoryViewModel viewModel = new(
                testDb.DbFactory,
                testDb.MergerService,
                testDb.GroupingService,
                testDb.NavigationService,
                testDb.LocalizationService,
                testDb.Logger
            );

            await viewModel.LoadHistoryAsync(character.Name);

            viewModel.AvailableGoals.Should().HaveCount(2);
            viewModel.SelectedGoalFilter.Should().NotBeNull();
            viewModel.Items.Should().HaveCount(1);
            viewModel.StatusMessage.Should().Be("History_EntriesLoaded");
        }

        // --- Test Helper Classes ---

        private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly System.Data.Common.DbConnection _connection;

            public TestDbContextFactory(System.Data.Common.DbConnection connection)
            {
                _connection = connection;
            }

            public AppDbContext CreateDbContext()
            {
                // Create new context with same connection
                DbContextOptionsBuilder<AppDbContext> builder = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection);

                return new AppDbContext(builder.Options);
            }
        }

        private sealed class MockNavigationService : INavigationService
        {
            public event Action<ViewModelBase>? Navigated
            {
                add { }
                remove { }
            }
            public ViewModelBase? CurrentViewModel => null;
            public bool CanGoBack => false;

            public Task NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
            {
                return Task.CompletedTask;
            }

            public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase { }
            public void GoBack() { }
        }

        private sealed class MockLocalizationService : ILocalizationService
        {
            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged
            {
                add { }
                remove { }
            }

            public string this[string key] => key;
            public System.Globalization.CultureInfo CurrentCulture => new("en");

            public void ChangeLanguage(string cultureCode) { }
            public List<string> GetAvailableLanguages() => ["en"];
        }

        private sealed class MockLogger : ILogger<HistoryViewModel>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
    }
}
