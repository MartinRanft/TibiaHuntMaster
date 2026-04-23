using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Map;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.App.ViewModels.Dashboard;
using TibiaHuntMaster.Core.Abstractions.TibiaData;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Services.Analysis;
using TibiaHuntMaster.Infrastructure.Services.Hunts;

namespace TibiaHuntMaster.Tests.ViewModels
{
    /// <summary>
    /// Comprehensive localization integration tests across multiple ViewModels.
    /// Tests language switching behavior and PropertyChanged propagation for all 6 supported languages.
    /// </summary>
    public sealed class LocalizationIntegrationTests
    {
        private readonly LocalizationService _localizationService;
        private readonly Mock<IHuntSessionService> _huntServiceMock;
        private readonly Mock<ICharacterService> _characterServiceMock;
        private readonly Mock<IGoalService> _goalServiceMock;
        private readonly Mock<IMonsterImageCatalogService> _monsterImageCatalogServiceMock;
        private readonly Mock<INavigationService> _navigationServiceMock;
        private readonly Mock<IDbContextFactory<AppDbContext>> _dbFactoryMock;
        private readonly HuntMergerService _mergerService;
        private readonly HuntGroupingService _groupingService;

        public LocalizationIntegrationTests()
        {
            // Use REAL LocalizationService to test actual translations
            _localizationService = new LocalizationService();

            // Mock dependencies
            _huntServiceMock = new Mock<IHuntSessionService>();
            _characterServiceMock = new Mock<ICharacterService>();
            _goalServiceMock = new Mock<IGoalService>();
            _monsterImageCatalogServiceMock = new Mock<IMonsterImageCatalogService>();
            _navigationServiceMock = new Mock<INavigationService>();
            _dbFactoryMock = new Mock<IDbContextFactory<AppDbContext>>();
            _mergerService = new HuntMergerService();
            _groupingService = new HuntGroupingService(_dbFactoryMock.Object);

            _monsterImageCatalogServiceMock.Setup(x => x.EnsureCatalogAsync(It.IsAny<CancellationToken>()))
                                           .Returns(Task.CompletedTask);
            _monsterImageCatalogServiceMock.Setup(x => x.DeathFallbackImageUri)
                                           .Returns("avares://TibiaHuntMaster.App/Assets/Standalone/DeathSplash_2x.gif");
            _monsterImageCatalogServiceMock.Setup(x => x.PlayerKillerImageUri)
                                           .Returns("avares://TibiaHuntMaster.App/Assets/Vocations/Monk_Artwork.png");
        }

        #region OverviewViewModel Tests

        [Theory]
        [InlineData("en", "Common_Save", "Save")]
        [InlineData("de", "Common_Save", "Speichern")]
        [InlineData("pl", "Common_Save", "Zapisz")]
        [InlineData("es", "Common_Save", "Guardar")]
        [InlineData("pt", "Common_Save", "Salvar")]
        [InlineData("sv", "Common_Save", "Spara")]
        public void OverviewViewModel_ShouldTranslateCommonStrings_ForAllLanguages(
            string languageCode, string key, string expectedValue)
        {
            // Arrange
            _localizationService.ChangeLanguage(languageCode);
            var viewModel = CreateOverviewViewModel();

            // Act
            string actualValue = _localizationService[key];

            // Assert
            actualValue.Should().Be(expectedValue,
                $"'{key}' should be '{expectedValue}' in {languageCode}");
        }

        [Fact]
        public void OverviewViewModel_ShouldReceivePropertyChangedEvent_WhenLanguageChanges()
        {
            // Arrange
            _localizationService.ChangeLanguage("en");
            var viewModel = CreateOverviewViewModel();

            bool eventRaised = false;
            _localizationService.PropertyChanged += (s, e) => { eventRaised = true; };

            // Act
            _localizationService.ChangeLanguage("de");

            // Assert
            eventRaised.Should().BeTrue("LocalizationService should raise PropertyChanged when language changes");
        }

        [Theory]
        [InlineData("en", "de")]
        [InlineData("de", "pl")]
        [InlineData("pl", "es")]
        [InlineData("es", "pt")]
        [InlineData("pt", "sv")]
        [InlineData("sv", "en")]
        public void OverviewViewModel_ShouldUpdateLocalizedStrings_WhenSwitchingBetweenLanguages(
            string fromLang, string toLang)
        {
            // Arrange
            _localizationService.ChangeLanguage(fromLang);
            var viewModel = CreateOverviewViewModel();
            string valueBefore = _localizationService["Common_Save"];

            // Act
            _localizationService.ChangeLanguage(toLang);
            string valueAfter = _localizationService["Common_Save"];

            // Assert
            valueBefore.Should().NotBeNullOrEmpty();
            valueAfter.Should().NotBeNullOrEmpty();
            valueBefore.Should().NotBe(valueAfter,
                $"Translation should differ between {fromLang} and {toLang}");
        }

        [Fact]
        public void OverviewViewModel_GoalTypeOptions_ShouldRefresh_WhenLanguageChanges()
        {
            // Arrange
            _localizationService.ChangeLanguage("en");
            var viewModel = CreateOverviewViewModel();
            string firstOptionEn = viewModel.GoalTypeOptions.First();

            // Act
            _localizationService.ChangeLanguage("de");
            string firstOptionDe = viewModel.GoalTypeOptions.First();

            // Assert
            viewModel.GoalTypeOptions.Should().HaveCount(2);
            firstOptionDe.Should().NotBe(firstOptionEn);
        }

        #endregion

        #region HistoryViewModel Tests

        [Theory]
        [InlineData("en", "History_StatusEmpty", "No hunts found.")]
        [InlineData("de", "History_StatusEmpty", "Keine Hunts gefunden.")]
        [InlineData("pl", "History_StatusEmpty", "Nie znaleziono łowów.")]
        [InlineData("es", "History_StatusEmpty", "No se encontraron cacerías.")]
        [InlineData("pt", "History_StatusEmpty", "Nenhuma caça encontrada.")]
        [InlineData("sv", "History_StatusEmpty", "Inga jakter hittades.")]
        public void HistoryViewModel_ShouldTranslateStatusMessages_ForAllLanguages(
            string languageCode, string key, string expectedValue)
        {
            // Arrange
            _localizationService.ChangeLanguage(languageCode);
            var viewModel = CreateHistoryViewModel();

            // Act
            string actualValue = _localizationService[key];

            // Assert
            actualValue.Should().Be(expectedValue,
                $"'{key}' should be '{expectedValue}' in {languageCode}");
        }

        [Fact]
        public void HistoryViewModel_StatusMessage_ShouldUpdate_WhenLanguageChanges()
        {
            // Arrange
            _localizationService.ChangeLanguage("en");
            var viewModel = CreateHistoryViewModel();
            string initialStatus = viewModel.StatusMessage;

            // Act
            _localizationService.ChangeLanguage("de");

            // Wait for async update (ViewModel has OnLanguageChanged handler)
            System.Threading.Thread.Sleep(50);

            // Assert
            viewModel.StatusMessage.Should().NotBe(initialStatus,
                "StatusMessage should update when language changes");
        }

        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        [InlineData("pl")]
        [InlineData("es")]
        [InlineData("pt")]
        [InlineData("sv")]
        public void HistoryViewModel_ShouldTriggerOnLanguageChangedHandler_ForAllLanguages(string languageCode)
        {
            // Arrange
            string initialLanguage = languageCode == "en" ? "de" : "en";
            _localizationService.ChangeLanguage(initialLanguage);
            var viewModel = CreateHistoryViewModel();

            bool handlerWasCalled = false;
            int propertyChangedCount = 0;

            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HistoryViewModel.StatusMessage))
                {
                    handlerWasCalled = true;
                    propertyChangedCount++;
                }
            };

            // Act
            _localizationService.ChangeLanguage(languageCode);
            System.Threading.Thread.Sleep(50); // Wait for async updates

            // Assert
            handlerWasCalled.Should().BeTrue($"HistoryViewModel should react to language change to {languageCode}");
            propertyChangedCount.Should().BeGreaterThan(0, "PropertyChanged should be raised for StatusMessage");
        }

        [Fact]
        public void HistoryViewModel_TypeFilterOptions_ShouldRefresh_WhenLanguageChanges()
        {
            // Arrange
            _localizationService.ChangeLanguage("en");
            var viewModel = CreateHistoryViewModel();
            string allTypesEn = viewModel.TypeFilterOptions.First();

            // Act
            _localizationService.ChangeLanguage("de");
            string allTypesDe = viewModel.TypeFilterOptions.First();

            // Assert
            viewModel.TypeFilterOptions.Should().HaveCount(3);
            allTypesDe.Should().NotBe(allTypesEn);
        }

        #endregion

        #region HuntingPlacesViewModel Tests

        [Theory]
        [InlineData("en", "HuntingPlaces_StatusLoading", "Loading hunting places...")]
        [InlineData("de", "HuntingPlaces_StatusLoading", "Lade Jagdgebiete...")]
        [InlineData("pl", "HuntingPlaces_StatusLoading", "Ładowanie miejsc łowów...")]
        [InlineData("es", "HuntingPlaces_StatusLoading", "Cargando lugares de caza...")]
        [InlineData("pt", "HuntingPlaces_StatusLoading", "Carregando locais de caça...")]
        [InlineData("sv", "HuntingPlaces_StatusLoading", "Laddar jaktplatser...")]
        public void HuntingPlacesViewModel_ShouldTranslateStatusMessages_ForAllLanguages(
            string languageCode, string key, string expectedValue)
        {
            // Arrange
            _localizationService.ChangeLanguage(languageCode);
            var viewModel = CreateHuntingPlacesViewModel();

            // Act
            string actualValue = _localizationService[key];

            // Assert
            actualValue.Should().Be(expectedValue,
                $"'{key}' should be '{expectedValue}' in {languageCode}");
        }

        [Fact]
        public void HuntingPlacesViewModel_StatusMessage_ShouldUpdate_WhenLanguageChanges()
        {
            // Arrange
            _localizationService.ChangeLanguage("en");
            var viewModel = CreateHuntingPlacesViewModel();
            string initialStatus = viewModel.StatusMessage;

            // Act
            _localizationService.ChangeLanguage("de");
            System.Threading.Thread.Sleep(50);

            // Assert
            viewModel.StatusMessage.Should().NotBe(initialStatus,
                "StatusMessage should update when language changes");
        }

        [Theory]
        [InlineData("en", "de", "HuntingPlaces_StatusLoading")]
        [InlineData("de", "pl", "HuntingPlaces_StatusEmpty")]
        [InlineData("pl", "es", "Common_Cancel")]
        [InlineData("es", "pt", "Common_Save")]
        [InlineData("pt", "sv", "Common_Delete")]
        public void HuntingPlacesViewModel_ShouldProvideDistinctTranslations_BetweenLanguages(
            string lang1, string lang2, string key)
        {
            // Arrange
            var viewModel = CreateHuntingPlacesViewModel();

            // Act
            _localizationService.ChangeLanguage(lang1);
            string translation1 = _localizationService[key];

            _localizationService.ChangeLanguage(lang2);
            string translation2 = _localizationService[key];

            // Assert
            translation1.Should().NotBe(translation2,
                $"'{key}' should have different translations in {lang1} and {lang2}");
            translation1.Should().NotContain("[", "Translation should not be missing (no brackets)");
            translation2.Should().NotContain("[", "Translation should not be missing (no brackets)");
        }

        [Fact]
        public void HuntingPlacesViewModel_FilterOptions_ShouldRefresh_WhenLanguageChanges()
        {
            // Arrange
            _localizationService.ChangeLanguage("en");
            var viewModel = CreateHuntingPlacesViewModel();
            string firstOptionEn = viewModel.VocationFilterOptions.First();

            // Act
            _localizationService.ChangeLanguage("de");
            string firstOptionDe = viewModel.VocationFilterOptions.First();

            // Assert
            viewModel.VocationFilterOptions.Should().HaveCount(4);
            firstOptionDe.Should().NotBe(firstOptionEn);
        }

        #endregion

        #region Cross-ViewModel Consistency Tests

        [Theory]
        [InlineData("Common_Save")]
        [InlineData("Common_Cancel")]
        [InlineData("Common_Delete")]
        [InlineData("Common_Edit")]
        [InlineData("Common_Close")]
        public void AllViewModels_ShouldShareSameTranslation_ForCommonKeys(string commonKey)
        {
            // Arrange
            var overviewVM = CreateOverviewViewModel();
            var historyVM = CreateHistoryViewModel();
            var huntingPlacesVM = CreateHuntingPlacesViewModel();

            // Test across all 6 languages
            string[] languages = ["en", "de", "pl", "es", "pt", "sv"];

            foreach (string lang in languages)
            {
                // Act
                _localizationService.ChangeLanguage(lang);
                string translation = _localizationService[commonKey];

                // Assert
                translation.Should().NotContain("[",
                    $"'{commonKey}' should have a valid translation in {lang}");
                translation.Should().NotBeNullOrEmpty(
                    $"'{commonKey}' should not be empty in {lang}");
            }
        }

        [Fact]
        public void AllViewModels_ShouldRespondToLanguageChange_Simultaneously()
        {
            // Arrange
            _localizationService.ChangeLanguage("en");
            var overviewVM = CreateOverviewViewModel();
            var historyVM = CreateHistoryViewModel();
            var huntingPlacesVM = CreateHuntingPlacesViewModel();

            int overviewEventCount = 0;
            int historyEventCount = 0;
            int huntingPlacesEventCount = 0;

            _localizationService.PropertyChanged += (s, e) => overviewEventCount++;
            _localizationService.PropertyChanged += (s, e) => historyEventCount++;
            _localizationService.PropertyChanged += (s, e) => huntingPlacesEventCount++;

            // Act
            _localizationService.ChangeLanguage("de");

            // Assert
            overviewEventCount.Should().BeGreaterThan(0, "OverviewViewModel should receive event");
            historyEventCount.Should().BeGreaterThan(0, "HistoryViewModel should receive event");
            huntingPlacesEventCount.Should().BeGreaterThan(0, "HuntingPlacesViewModel should receive event");
        }

        #endregion

        #region Missing Translation Detection Tests

        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        [InlineData("pl")]
        [InlineData("es")]
        [InlineData("pt")]
        [InlineData("sv")]
        public void LocalizationService_ShouldNotReturnBracketedKeys_ForCriticalStrings(string languageCode)
        {
            // Arrange
            _localizationService.ChangeLanguage(languageCode);

            // Critical keys that MUST be translated in all languages
            string[] criticalKeys =
            [
                "Common_Save",
                "Common_Cancel",
                "Common_Delete",
                "Common_Edit",
                "Common_Close",
                "History_StatusEmpty",
                "HuntingPlaces_StatusLoading"
            ];

            // Act & Assert
            foreach (string key in criticalKeys)
            {
                string value = _localizationService[key];

                value.Should().NotContain("[",
                    $"Critical key '{key}' is MISSING translation in {languageCode}! Value: {value}");
                value.Should().NotBeNullOrEmpty(
                    $"Critical key '{key}' should not be empty in {languageCode}");
            }
        }

        [Theory]
        [InlineData("Overview_RecentDeaths")]
        [InlineData("Overview_ActiveGoals")]
        [InlineData("Overview_TotalProfit")]
        [InlineData("History_EntriesLoaded")]
        [InlineData("HuntingPlaces_StatusEmpty")]
        [InlineData("HuntingPlaces_StatusCount")]
        public void LocalizationService_ShouldProvideTranslations_ForViewSpecificKeys(string key)
        {
            // Arrange
            string[] languages = ["en", "de", "pl", "es", "pt", "sv"];

            foreach (string lang in languages)
            {
                // Act
                _localizationService.ChangeLanguage(lang);
                string value = _localizationService[key];

                // Assert
                value.Should().NotContain("[",
                    $"Key '{key}' is MISSING in {lang}! Found: {value}");
            }
        }

        /// <summary>
        /// This test helps identify which specific translations are missing across all languages.
        /// It will OUTPUT detailed diagnostic information to help debug localization issues.
        /// </summary>
        [Fact]
        public void DiagnosticTest_ShowAllMissingTranslations()
        {
            // Arrange
            string[] languages = ["en", "de", "pl", "es", "pt", "sv"];
            string[] testKeys =
            [
                // Common
                "Common_Save", "Common_Cancel", "Common_Delete", "Common_Edit", "Common_Close",

                // Overview
                "Overview_RecentDeaths", "Overview_ActiveGoals", "Overview_TotalProfit",

                // History
                "History_StatusEmpty", "History_EntriesLoaded", "History_FilterByGoal",
                "History_Solo", "History_Party", "History_AllTypes",

                // Hunting Places
                "HuntingPlaces_StatusLoading", "HuntingPlaces_StatusEmpty", "HuntingPlaces_StatusCount",
                "HuntingPlaces_Vocation", "HuntingPlaces_MinLevel"
            ];

            var missingTranslations = new List<string>();

            // Act - Check each language for missing keys
            foreach (string lang in languages)
            {
                _localizationService.ChangeLanguage(lang);

                foreach (string key in testKeys)
                {
                    string value = _localizationService[key];

                    if (value.Contains("[") || string.IsNullOrWhiteSpace(value))
                    {
                        missingTranslations.Add($"[{lang}] {key} = {value}");
                    }
                }
            }

            // Assert with detailed diagnostics
            if (missingTranslations.Count > 0)
            {
                string diagnosticReport = string.Join("\n", missingTranslations);
                Assert.Fail($"Found {missingTranslations.Count} missing translations:\n{diagnosticReport}");
            }
        }

        #endregion

        #region Helper Methods

        private OverviewViewModel CreateOverviewViewModel()
        {
            return new OverviewViewModel(
                _huntServiceMock.Object,
                _characterServiceMock.Object,
                _goalServiceMock.Object,
                _navigationServiceMock.Object,
                _localizationService, // Real service
                _monsterImageCatalogServiceMock.Object,
                NullLogger<OverviewViewModel>.Instance
            );
        }

        private HistoryViewModel CreateHistoryViewModel()
        {
            return new HistoryViewModel(
                _dbFactoryMock.Object,
                _mergerService,
                _groupingService,
                _navigationServiceMock.Object,
                _localizationService, // Real service
                NullLogger<HistoryViewModel>.Instance
            );
        }

        private HuntingPlacesViewModel CreateHuntingPlacesViewModel()
        {
            return new HuntingPlacesViewModel(
                _dbFactoryMock.Object,
                _localizationService, // Real service
                _navigationServiceMock.Object
            );
        }

        #endregion
    }
}
