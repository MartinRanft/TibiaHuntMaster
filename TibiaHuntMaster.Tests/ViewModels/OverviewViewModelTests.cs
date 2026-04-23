using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Map;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.App.ViewModels.Dashboard;
using TibiaHuntMaster.Core.Abstractions.TibiaData;
using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Infrastructure.Data.Entities.Character;
using TibiaHuntMaster.Infrastructure.Services.Analysis;
using TibiaHuntMaster.Infrastructure.Services.Hunts;

namespace TibiaHuntMaster.Tests.ViewModels
{
    public sealed class OverviewViewModelTests
    {
        private readonly Mock<IHuntSessionService> _huntServiceMock;
        private readonly Mock<ICharacterService> _characterServiceMock;
        private readonly Mock<IGoalService> _goalServiceMock;
        private readonly Mock<INavigationService> _navigationServiceMock;
        private readonly Mock<ILocalizationService> _localizationServiceMock;
        private readonly Mock<IMonsterImageCatalogService> _monsterImageCatalogServiceMock;

        public OverviewViewModelTests()
        {
            _huntServiceMock = new Mock<IHuntSessionService>();
            _characterServiceMock = new Mock<ICharacterService>();
            _goalServiceMock = new Mock<IGoalService>();
            _navigationServiceMock = new Mock<INavigationService>();
            _localizationServiceMock = new Mock<ILocalizationService>();
            _monsterImageCatalogServiceMock = new Mock<IMonsterImageCatalogService>();
            _localizationServiceMock.Setup(x => x[It.IsAny<string>()]).Returns((string key) => key);
            _monsterImageCatalogServiceMock.Setup(x => x.EnsureCatalogAsync(It.IsAny<CancellationToken>()))
                                           .Returns(Task.CompletedTask);
            _monsterImageCatalogServiceMock.Setup(x => x.DeathFallbackImageUri)
                                           .Returns("avares://TibiaHuntMaster.App/Assets/Standalone/DeathSplash_2x.gif");
            _monsterImageCatalogServiceMock.Setup(x => x.PlayerKillerImageUri)
                                           .Returns("avares://TibiaHuntMaster.App/Assets/Vocations/Monk_Artwork.png");
        }

        private OverviewViewModel CreateViewModel()
        {
            return new OverviewViewModel(
                _huntServiceMock.Object,
                _characterServiceMock.Object,
                _goalServiceMock.Object,
                _navigationServiceMock.Object,
                _localizationServiceMock.Object,
                _monsterImageCatalogServiceMock.Object,
                NullLogger<OverviewViewModel>.Instance
            );
        }

        [Fact]
        public void Constructor_ShouldInitializeViewModel()
        {
            // Act
            OverviewViewModel viewModel = CreateViewModel();

            // Assert
            viewModel.Should().NotBeNull();
            viewModel.ActiveGoals.Should().NotBeNull();
            viewModel.CompletedGoals.Should().NotBeNull();
            viewModel.RecentDeaths.Should().NotBeNull();
            viewModel.IsLoading.Should().BeFalse();
        }

        [Fact]
        public void DialogMode_ShouldStartAsNone()
        {
            // Act
            OverviewViewModel viewModel = CreateViewModel();

            // Assert
            viewModel.DialogMode.Should().Be(GoalDialogMode.None);
            viewModel.IsDialogVisible.Should().BeFalse();
        }

        [Fact]
        public void GoalTitleInput_ShouldUpdateProperty()
        {
            // Arrange
            OverviewViewModel viewModel = CreateViewModel();
            string title = "Reach Level 100";

            // Act
            viewModel.GoalTitleInput = title;

            // Assert
            viewModel.GoalTitleInput.Should().Be(title);
        }

        [Fact]
        public void GoalTargetInput_ShouldUpdateProperty()
        {
            // Arrange
            OverviewViewModel viewModel = CreateViewModel();
            long target = 1000000L;

            // Act
            viewModel.GoalTargetInput = target;

            // Assert
            viewModel.GoalTargetInput.Should().Be(target);
        }

        [Fact]
        public void Character_ShouldUpdateProperty()
        {
            // Arrange
            OverviewViewModel viewModel = CreateViewModel();
            Character character = new Character { Name = "Test", Level = 50 };

            // Act
            viewModel.Character = character;

            // Assert
            viewModel.Character.Should().BeSameAs(character);
        }

        [Fact]
        public void IsLoading_ShouldUpdateProperty()
        {
            // Arrange
            OverviewViewModel viewModel = CreateViewModel();

            // Act
            viewModel.IsLoading = true;

            // Assert
            viewModel.IsLoading.Should().BeTrue();
        }

        [Fact]
        public void ValidationError_ShouldStartEmpty()
        {
            // Arrange & Act
            OverviewViewModel viewModel = CreateViewModel();

            // Assert
            viewModel.ValidationError.Should().BeEmpty();
        }

        [Fact]
        public void ShowCelebration_ShouldUpdateProperty()
        {
            // Arrange
            OverviewViewModel viewModel = CreateViewModel();

            // Act
            viewModel.ShowCelebration = true;

            // Assert
            viewModel.ShowCelebration.Should().BeTrue();
        }

        [Fact]
        public void OpenAddGoal_ShouldPrefillBaseLevel_FromCurrentCharacterLevel()
        {
            // Arrange
            OverviewViewModel viewModel = CreateViewModel();
            viewModel.Character = new Character { Id = 7, Name = "Test", Level = 612 };

            // Act
            viewModel.OpenAddGoal();

            // Assert
            viewModel.GoalBaseLevelInput.Should().Be(612);
        }

        [Fact]
        public async Task SubmitDialog_CreateLevelGoal_ShouldUseCustomBaseLevelAsStartValue()
        {
            // Arrange
            _goalServiceMock
            .Setup(x => x.GetGoalsForCharacterAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<GoalProgressResult>());
            _goalServiceMock
            .Setup(x => x.AddGoalAsync(It.IsAny<CharacterGoalEntity>()))
            .Returns(Task.CompletedTask);

            OverviewViewModel viewModel = CreateViewModel();
            viewModel.Character = new Character { Id = 42, Name = "Leveler", Level = 600 };
            viewModel.OpenAddGoal();
            viewModel.NewGoalTypeIndex = 0; // Level goal
            viewModel.GoalTitleInput = "To 650";
            viewModel.GoalBaseLevelInput = 580;
            viewModel.GoalTargetInput = 650;

            // Act
            await viewModel.SubmitDialog();

            // Assert
            _goalServiceMock.Verify(x => x.AddGoalAsync(It.Is<CharacterGoalEntity>(g =>
            g.CharacterId == 42 &&
            g.Type == GoalType.Level &&
            g.StartValue == 580 &&
            g.TargetValue == 650)), Times.Once);
            viewModel.ValidationError.Should().BeEmpty();
        }
    }
}
