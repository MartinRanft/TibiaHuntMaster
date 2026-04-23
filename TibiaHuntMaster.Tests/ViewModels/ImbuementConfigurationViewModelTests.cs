using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.App.ViewModels.Configuration;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Services.Analysis;

namespace TibiaHuntMaster.Tests.ViewModels
{
    public sealed class ImbuementConfigurationViewModelTests
    {
        private readonly Mock<IDbContextFactory<AppDbContext>> _dbFactoryMock;
        private readonly Mock<IImbuementCalculatorService> _calcServiceMock;
        private readonly Mock<INavigationService> _navigationServiceMock;

        public ImbuementConfigurationViewModelTests()
        {
            _dbFactoryMock = new Mock<IDbContextFactory<AppDbContext>>();
            _calcServiceMock = new Mock<IImbuementCalculatorService>();
            _navigationServiceMock = new Mock<INavigationService>();
        }

        private ImbuementConfigurationViewModel CreateViewModel()
        {
            return new ImbuementConfigurationViewModel(
                _dbFactoryMock.Object,
                _calcServiceMock.Object,
                _navigationServiceMock.Object
            );
        }

        [Fact]
        public void Constructor_ShouldInitializeViewModel()
        {
            // Act
            ImbuementConfigurationViewModel viewModel = CreateViewModel();

            // Assert
            viewModel.Should().NotBeNull();
            viewModel.FilteredGroups.Should().NotBeNull();
            viewModel.ItemPrices.Should().NotBeNull();
            viewModel.SearchText.Should().BeEmpty();
            viewModel.SelectedCategoryIndex.Should().Be(0);
        }

        [Fact]
        public void SearchText_ShouldUpdateProperty()
        {
            // Arrange
            ImbuementConfigurationViewModel viewModel = CreateViewModel();
            string searchText = "vampirism";

            // Act
            viewModel.SearchText = searchText;

            // Assert
            viewModel.SearchText.Should().Be(searchText);
        }

        [Fact]
        public void SelectedCategoryIndex_ShouldUpdateProperty()
        {
            // Arrange
            ImbuementConfigurationViewModel viewModel = CreateViewModel();

            // Act
            viewModel.SelectedCategoryIndex = 2;

            // Assert
            viewModel.SelectedCategoryIndex.Should().Be(2);
        }

        [Fact]
        public void UseBlankScrolls_ShouldUpdateProperty()
        {
            // Arrange
            ImbuementConfigurationViewModel viewModel = CreateViewModel();

            // Act
            viewModel.UseBlankScrolls = true;

            // Assert
            viewModel.UseBlankScrolls.Should().BeTrue();
        }

        [Fact]
        public void IsLoading_ShouldStartAsFalse()
        {
            // Arrange & Act
            ImbuementConfigurationViewModel viewModel = CreateViewModel();

            // Assert
            viewModel.IsLoading.Should().BeFalse();
        }

        [Fact]
        public void FilteredGroups_ShouldBeInitializedEmpty()
        {
            // Arrange & Act
            ImbuementConfigurationViewModel viewModel = CreateViewModel();

            // Assert
            viewModel.FilteredGroups.Should().BeEmpty();
        }

        [Fact]
        public void ItemPrices_ShouldBeInitializedEmpty()
        {
            // Arrange & Act
            ImbuementConfigurationViewModel viewModel = CreateViewModel();

            // Assert
            viewModel.ItemPrices.Should().BeEmpty();
        }
    }
}
