using System.ComponentModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.App.ViewModels;
using TibiaHuntMaster.App.ViewModels.Dashboard;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.Hunts;

namespace TibiaHuntMaster.Tests.Integration
{
    public sealed class NavigationHistoryIntegrationTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly ServiceProvider _serviceProvider;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public NavigationHistoryIntegrationTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"thm-nav-history-{Guid.NewGuid():N}.db");
            _dbFactory = new SqliteFileDbContextFactory(_dbPath);

            using AppDbContext initDb = _dbFactory.CreateDbContext();
            initDb.Database.EnsureCreated();

            ServiceCollection services = new();
            services.AddSingleton(_dbFactory);
            services.AddSingleton<HuntMergerService>();
            services.AddSingleton<HuntGroupingService>();
            services.AddSingleton<ILocalizationService, TestLocalizationService>();
            services.AddSingleton<ILogger<HistoryViewModel>>(NullLogger<HistoryViewModel>.Instance);
            services.AddSingleton<INavigationService>(sp => new NavigationService(sp));
            services.AddTransient<HistoryViewModel>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task NavigateTo_HistoryViewModel_ShouldLoadHistory_FromNavigationParameter()
        {
            // Arrange
            await SeedCharacterWithHistoryAsync("IntegrationKnight");
            INavigationService navigationService = _serviceProvider.GetRequiredService<INavigationService>();
            object historyParameter = CreateHistoryWithCharacterParameter("integrationknight");

            // Act
            navigationService.NavigateTo<HistoryViewModel>(historyParameter);
            navigationService.CurrentViewModel.Should().BeOfType<HistoryViewModel>();

            HistoryViewModel viewModel = (HistoryViewModel)navigationService.CurrentViewModel!;
            await WaitUntilAsync(() =>
            viewModel.Items.Count > 0 ||
            viewModel.StatusMessage.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
            viewModel.StatusMessage.Contains("not found", StringComparison.OrdinalIgnoreCase));

            // Assert
            viewModel.Items.Should().HaveCount(1, $"status was '{viewModel.StatusMessage}'");
            viewModel.Items[0].Session.Should().NotBeNull();
            viewModel.StatusMessage.Should().Contain("1");
        }

        private async Task SeedCharacterWithHistoryAsync(string characterName)
        {
            await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
            CharacterEntity character = new()
            {
                Name = characterName,
                World = "Antica",
                Vocation = "Elite Knight",
                Level = 320
            };
            db.Characters.Add(character);
            await db.SaveChangesAsync();

            HuntSessionEntity hunt = new()
            {
                CharacterId = character.Id,
                SessionStartTime = DateTimeOffset.UtcNow.AddHours(-1),
                ImportedAt = DateTimeOffset.UtcNow,
                Loot = 250000,
                Supplies = 90000,
                Balance = 160000,
                XpGain = 1500000,
                Duration = TimeSpan.FromMinutes(75)
            };
            db.HuntSessions.Add(hunt);
            await db.SaveChangesAsync();
        }

        private static object CreateHistoryWithCharacterParameter(string characterName)
        {
            Type? parameterType = typeof(NavigationService).Assembly
                                                           .GetType("TibiaHuntMaster.App.Services.Navigation.NavigationParameters+HistoryWithCharacter");

            parameterType.Should().NotBeNull("History navigation parameter type must exist");
            object? instance = Activator.CreateInstance(parameterType!, characterName);
            instance.Should().NotBeNull("History navigation parameter should be constructible");
            return instance!;
        }

        private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000, int pollMs = 25)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while(DateTime.UtcNow < deadline)
            {
                if(condition())
                {
                    return;
                }

                await Task.Delay(pollMs);
            }
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();

            try
            {
                if(File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests.
            }
        }

        private sealed class SqliteFileDbContextFactory(string dbPath) : IDbContextFactory<AppDbContext>
        {
            private readonly string _connectionString = $"Data Source={dbPath}";

            public AppDbContext CreateDbContext()
            {
                DbContextOptionsBuilder<AppDbContext> builder = new();
                builder.UseSqlite(_connectionString);
                return new AppDbContext(builder.Options);
            }
        }

        private sealed class TestLocalizationService : ILocalizationService
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public string this[string key] => key switch
            {
                "History_StatusEmpty" => "No hunts found.",
                "History_EntriesLoaded" => "{0} entries loaded.",
                "History_AllHuntsNoGoal" => "All Hunts (No Goal)",
                "History_StatusFilteredEmpty" => "No visible hunts.",
                _ => key
            };

            public System.Globalization.CultureInfo CurrentCulture => new("en");

            public void ChangeLanguage(string cultureCode)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            }

            public List<string> GetAvailableLanguages() => ["en"];
        }
    }
}
