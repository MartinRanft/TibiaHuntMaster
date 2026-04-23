using FluentAssertions;
using Moq;
using TibiaHuntMaster.App.Services.Map;
using TibiaHuntMaster.App.Services.ErrorHandling;
using TibiaHuntMaster.App.ViewModels.Dashboard;
using TibiaHuntMaster.Core.Abstractions.Map;
using CoreMap = TibiaHuntMaster.Core.Map.Map;

namespace TibiaHuntMaster.Tests.ViewModels
{
    public sealed class MinimapViewModelTests
    {
        private readonly Mock<IMapSectionService> _mapSectionServiceMock;
        private readonly Mock<IMinimapTileCatalog> _tileCatalogMock;
        private readonly Mock<IMinimapMarkerService> _markerServiceMock;
        private readonly Mock<IMonsterSpawnQueryService> _spawnQueryServiceMock;
        private readonly Mock<IMonsterImageCatalogService> _monsterImageCatalogServiceMock;
        private readonly Mock<IErrorHandlingService> _errorHandlingMock;

        public MinimapViewModelTests()
        {
            _mapSectionServiceMock = new Mock<IMapSectionService>();
            _tileCatalogMock = new Mock<IMinimapTileCatalog>();
            _markerServiceMock = new Mock<IMinimapMarkerService>();
            _spawnQueryServiceMock = new Mock<IMonsterSpawnQueryService>();
            _monsterImageCatalogServiceMock = new Mock<IMonsterImageCatalogService>();
            _errorHandlingMock = new Mock<IErrorHandlingService>();

            _mapSectionServiceMock
            .Setup(x => x.GetSection(It.IsAny<CoreMap.MapSectionRequest>()))
            .Returns((CoreMap.MapSectionRequest request) => new CoreMap.MapSection(
                OriginX: request.CenterX - (request.Width / 2),
                OriginY: request.CenterY - (request.Height / 2),
                Width: request.Width,
                Height: request.Height,
                Z: request.Z,
                Layer: request.Layer,
                Bounds: new CoreMap.MapBounds(0, 0, 0, 0, request.Z),
                Tiles: Array.Empty<CoreMap.MapSectionTile>()));

            _markerServiceMock
            .Setup(x => x.GetAllMarkers())
            .Returns(Array.Empty<CoreMap.MinimapMarker>());

            _markerServiceMock
            .Setup(x => x.GetMarkersInBounds(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<byte>()))
            .Returns(Array.Empty<CoreMap.MinimapMarker>());

            _spawnQueryServiceMock
            .Setup(x => x.GetSpawnsInBoundsAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<byte>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CoreMap.MonsterSpawnMarker>());

            _spawnQueryServiceMock
            .Setup(x => x.SearchMonsterNamesAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

            _monsterImageCatalogServiceMock
            .Setup(x => x.EnsureCatalogAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        }

        private MinimapViewModel CreateViewModel() => new(
            _mapSectionServiceMock.Object,
            _tileCatalogMock.Object,
            _markerServiceMock.Object,
            _spawnQueryServiceMock.Object,
            _monsterImageCatalogServiceMock.Object,
            _errorHandlingMock.Object);

        [Theory]
        [InlineData("129.122,128.48,7", 33146, 32816, (byte)7)]
        [InlineData("129.195,128.16,7", 33219, 32784, (byte)7)]
        [InlineData("33194,32688,7", 33194, 32688, (byte)7)]
        public async Task JumpToCoordinates_ShouldParseWikiAndInternalFormats(string input, int expectedX, int expectedY, byte expectedZ)
        {
            // Arrange
            MinimapViewModel viewModel = CreateViewModel();
            viewModel.JumpCoordinatesInput = input;

            // Act
            await viewModel.JumpToCoordinatesCommand.ExecuteAsync(null);

            // Assert
            viewModel.CenterX.Should().Be(expectedX);
            viewModel.CenterY.Should().Be(expectedY);
            viewModel.Z.Should().Be(expectedZ);
            viewModel.CursorWorldX.Should().Be(expectedX);
            viewModel.CursorWorldY.Should().Be(expectedY);
            viewModel.CursorWorldZ.Should().Be(expectedZ);
            viewModel.Zoom.Should().Be(1.0);
            viewModel.PanX.Should().Be(0);
            viewModel.PanY.Should().Be(0);
        }
    }
}
