using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.App.Services;
using TibiaHuntMaster.App.Services.Diagnostics;
using TibiaHuntMaster.App.Services.Map;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.App.Services.Summaries;
using TibiaHuntMaster.App.ViewModels;
using TibiaHuntMaster.App.ViewModels.Analyzer;
using TibiaHuntMaster.App.ViewModels.Configuration;
using TibiaHuntMaster.App.ViewModels.Dashboard;
using TibiaHuntMaster.App.ViewModels.Selection;
using TibiaHuntMaster.App.ViewModels.Startup;
using TibiaHuntMaster.Core.Abstractions.TibiaData;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Extensions;
using TibiaHuntMaster.Infrastructure.Http.TibiaData;
using TibiaHuntMaster.Infrastructure.Http.TibiaPal;
using TibiaHuntMaster.Infrastructure.Services.Analysis;
using TibiaHuntMaster.Infrastructure.Services.Hunts;
using TibiaHuntMaster.Infrastructure.Services.Map;
using TibiaHuntMaster.Infrastructure.Services.Parsing;
using TibiaHuntMaster.Infrastructure.Services.System;
using TibiaHuntMaster.Infrastructure.Services.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.TibiaPal;

namespace TibiaHuntMaster.App.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static void AddTibiaHuntMasterServices(this IServiceCollection services)
        {
            AppDataPaths appDataPaths = new();
            appDataPaths.EnsureDirectories();
            services.AddSingleton(appDataPaths);

            services.AddSingleton<ILoggerProvider, RollingFileLoggerProvider>();
            services.AddLogging(builder =>
            {
#if DEBUG
                builder.SetMinimumLevel(LogLevel.Debug);
#else
                builder.SetMinimumLevel(LogLevel.Information);
#endif
                builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                builder.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            });

            // 2. Datenbank-Factory registrieren
            services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={appDataPaths.DatabasePath}"));

            // 3. HTTP Clients registrieren
            services.AddHttpClient<TibiaDataClient>();
            services.AddHttpClient<TibiaPalClient>();
            services.AddContentInfrastructure();

            // 4. Core Services registrieren

            // Parser (Stateless -> Singleton)
            services.AddSingleton<HuntAnalyzerParser>();
            services.AddSingleton<TeamHuntParser>();

            // Analysis Services
            services.AddSingleton<ILogDetectorService, LogDetectorService>();
            services.AddSingleton<IHuntComparisonService, HuntComparisonService>();
            services.AddSingleton<ILootAnalysisService, LootAnalysisService>();
            services.AddSingleton<IHuntSessionVerificationService, HuntSessionVerificationService>();
            services.AddSingleton<IGlossarySearchService, GlossarySearchService>();
            services.AddSingleton<IProgressInsightsService, ProgressInsightsService>();

            // Logic / Import Services
            services.AddSingleton<ICharacterService, CharacterService>();
            services.AddSingleton<TibiaPalService>();
            services.AddSingleton<IHuntSessionService, HuntSessionService>();
            services.AddSingleton<ITeamHuntService, TeamHuntService>();

            // System Services
            services.AddSingleton<ICreatureSyncService, CreatureSyncService>();
            services.AddSingleton<BoostedCreatureMonitor>();
            services.AddSingleton<IGoalService, GoalService>();
            services.AddSingleton<TibiaPathService>();
            services.AddSingleton<LocalEventsService>();
            services.AddSingleton<ClipboardMonitorService>();
            services.AddSingleton<MonsterSpawnSeedService>();

            // Navigation Service
            services.AddSingleton<INavigationService, NavigationService>();

            // Theme Service
            services.AddSingleton<ThemeService>();
            services.AddSingleton<UserPreferencesService>();
            services.AddSingleton<IDiagnosticsService, DiagnosticsService>();
            services.AddSingleton<IFileRevealService, FileRevealService>();
            services.AddSingleton<AppExceptionMonitor>();
            services.AddSingleton<IHuntSummaryGeneratorService, HuntSummaryGeneratorService>();

            // Localization Service
            services.AddSingleton<Services.Localization.ILocalizationService, Services.Localization.LocalizationService>();
            services.AddSingleton<Services.ErrorHandling.IErrorHandlingService, Services.ErrorHandling.ErrorHandlingService>();

            // --- NEUE SERVICES (Hunt Grouping & Imbuements) ---
            services.AddSingleton<HuntMergerService>();
            services.AddSingleton<HuntGroupingService>();

            // Hier fehlte der Calculator:
            services.AddSingleton<IImbuementCalculatorService, ImbuementCalculatorService>();
            services.AddSingleton<ImbuementSeedService>();
            
            // Map Services
            services.AddSingleton<Core.Abstractions.Map.IMinimapTileCatalog, AvaresMinimapTileCatalog>();
            services.AddSingleton<Core.Abstractions.Map.IMapSectionService, MapSectionService>();
            services.AddSingleton<Core.Abstractions.Map.IMinimapMarkerService, AvaresMinimapMarkerService>();
            services.AddSingleton<Core.Abstractions.Map.IMonsterSpawnQueryService, MonsterSpawnQueryService>();
            services.AddSingleton<IMonsterImageCatalogService, MonsterImageCatalogService>();

            // 5. ViewModels registrieren
            services.AddTransient<MainWindowViewModel>();

            services.AddTransient<SetupViewModel>();
            services.AddTransient<CharacterSelectionViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<OverviewViewModel>();
            services.AddTransient<ProgressViewModel>();
            services.AddTransient<EconomyViewModel>();
            services.AddTransient<HuntAnalyzerViewModel>();
            services.AddTransient<HistoryViewModel>();
            services.AddTransient<HuntingPlacesViewModel>();
            services.AddTransient<GoalHistoryViewModel>();
            services.AddTransient<MinimapViewModel>();

            // NEU: Der Config Screen muss auch registriert sein
            services.AddTransient<ImbuementConfigurationViewModel>();

            services.AddSingleton<DataStatusService>();
            services.AddSingleton<BackgroundSyncWorker>();
        }
    }
}
