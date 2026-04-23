using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TibiaHuntMaster.App.Services;
using TibiaHuntMaster.App.Services.Localization;

namespace TibiaHuntMaster.Tests.Services
{
    public sealed class LocalizationServiceTests
    {
        [Fact]
        public void Constructor_ShouldInitializeService()
        {
            // Act
            LocalizationService service = new LocalizationService();

            // Assert
            service.Should().NotBeNull();
            service.CurrentCulture.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldDefaultToEnglish_WhenNoPreferenceExists()
        {
            // Act
            LocalizationService service = new LocalizationService();

            // Assert
            service.CurrentCulture.TwoLetterISOLanguageName.Should().Be("en");
        }

        [Fact]
        public void GetAvailableLanguages_ShouldReturnAllSupportedLanguages()
        {
            // Arrange
            LocalizationService service = new LocalizationService();

            // Act
            List<string> languages = service.GetAvailableLanguages();

            // Assert
            languages.Should().Contain("en");
            languages.Should().Contain("de");
            languages.Should().Contain("pl");
            languages.Should().Contain("es");
            languages.Should().Contain("pt");
            languages.Should().Contain("sv");
            languages.Should().HaveCount(6);
        }

        [Fact]
        public void Indexer_ShouldReturnLocalizedString_ForValidKey()
        {
            // Arrange
            LocalizationService service = new LocalizationService();
            service.ChangeLanguage("en");

            // Act
            string value = service["Common_Save"];

            // Assert
            value.Should().NotBeNullOrEmpty();
            value.Should().Be("Save");
        }

        [Fact]
        public void Indexer_ShouldReturnKeyInBrackets_ForInvalidKey()
        {
            // Arrange
            LocalizationService service = new LocalizationService();

            // Act
            string value = service["NonExistentKey"];

            // Assert
            value.Should().Be("[NonExistentKey]");
        }

        [Fact]
        public void ChangeLanguage_ShouldUpdateCurrentCulture()
        {
            // Arrange
            LocalizationService service = new LocalizationService();
            service.ChangeLanguage("en");  // Ensure we start with English
            string originalCulture = service.CurrentCulture.TwoLetterISOLanguageName;

            // Act
            service.ChangeLanguage("de");

            // Assert
            service.CurrentCulture.TwoLetterISOLanguageName.Should().Be("de");
            service.CurrentCulture.TwoLetterISOLanguageName.Should().NotBe(originalCulture);
        }

        [Fact]
        public void ChangeLanguage_ShouldUpdateLocalizedStrings()
        {
            // Arrange
            LocalizationService service = new LocalizationService();
            service.ChangeLanguage("en");
            string englishValue = service["Common_Save"];

            // Act
            service.ChangeLanguage("de");
            string germanValue = service["Common_Save"];

            // Assert
            englishValue.Should().Be("Save");
            germanValue.Should().Be("Speichern");
            englishValue.Should().NotBe(germanValue);
        }

        [Fact]
        public void ChangeLanguage_ShouldNotRaisePropertyChanged_WhenLanguageIsSame()
        {
            // Arrange
            LocalizationService service = new LocalizationService();
            service.ChangeLanguage("en");

            bool eventRaised = false;
            service.PropertyChanged += (s, e) => { eventRaised = true; };

            // Act
            service.ChangeLanguage("en");

            // Assert
            eventRaised.Should().BeFalse();
        }

        [Fact]
        public void ChangeLanguage_ShouldRaisePropertyChanged_WhenLanguageChanges()
        {
            // Arrange
            LocalizationService service = new LocalizationService();
            service.ChangeLanguage("en");

            bool eventRaised = false;
            service.PropertyChanged += (s, e) => { eventRaised = true; };

            // Act
            service.ChangeLanguage("de");

            // Assert
            eventRaised.Should().BeTrue();
        }

        [Theory]
        [InlineData("Common_Cancel", "Cancel", "Abbrechen")]
        [InlineData("Common_Delete", "Delete", "Löschen")]
        [InlineData("Common_Edit", "Edit", "Bearbeiten")]
        [InlineData("Common_Close", "Close", "Schließen")]
        public void Localization_ShouldProvideCorrectTranslations(string key, string expectedEnglish, string expectedGerman)
        {
            // Arrange
            LocalizationService service = new LocalizationService();

            // Act & Assert - English
            service.ChangeLanguage("en");
            service[key].Should().Be(expectedEnglish);

            // Act & Assert - German
            service.ChangeLanguage("de");
            service[key].Should().Be(expectedGerman);
        }

        [Theory]
        [InlineData("pl", "Common_Save", "Zapisz")]
        [InlineData("pl", "Common_Cancel", "Anuluj")]
        [InlineData("es", "Common_Save", "Guardar")]
        [InlineData("es", "Common_Cancel", "Cancelar")]
        [InlineData("pt", "Common_Save", "Salvar")]
        [InlineData("pt", "Common_Cancel", "Cancelar")]
        [InlineData("sv", "Common_Save", "Spara")]
        [InlineData("sv", "Common_Cancel", "Avbryt")]
        public void Localization_ShouldProvideCorrectTranslations_ForNewLanguages(string languageCode, string key, string expectedValue)
        {
            // Arrange
            LocalizationService service = new LocalizationService();

            // Act
            service.ChangeLanguage(languageCode);
            string value = service[key];

            // Assert
            value.Should().Be(expectedValue);
        }

        [Fact]
        public void LanguagePreference_ShouldPersistAcrossServiceInstances()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), "thm-loc-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string preferencesFile = Path.Combine(tempDir, "preferences.json");

            try
            {
                UserPreferencesService firstPreferences = new UserPreferencesService(
                    NullLogger<UserPreferencesService>.Instance,
                    preferencesFile);
                LocalizationService firstService = new LocalizationService(firstPreferences);
                firstService.ChangeLanguage("de");

                // Act
                UserPreferencesService secondPreferences = new UserPreferencesService(
                    NullLogger<UserPreferencesService>.Instance,
                    preferencesFile);
                LocalizationService secondService = new LocalizationService(secondPreferences);

                // Assert
                secondService.CurrentCulture.TwoLetterISOLanguageName.Should().Be("de");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
    }
}
