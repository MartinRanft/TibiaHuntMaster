using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;

using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using TibiaHuntMaster.App.Services;
using TibiaHuntMaster.App.Services.ErrorHandling;
using TibiaHuntMaster.App.Services.Map;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.Core.Abstractions.Map;
using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Core.Security;

namespace TibiaHuntMaster.App.ViewModels.Dashboard
{
    
    public sealed class MinimapTileViewModel
    {
        public double X { get; }
        public double Y { get; }
        public Bitmap Image { get; }

        public MinimapTileViewModel(double x, double y, Bitmap image)
        {
            X = x;
            Y = y;
            Image = image;
        }
    }
    
    public sealed class MinimapMarkerViewModel(double x, double y, string? tooltip, string glyph, IBrush foreground)
    {
        public double X { get; } = x;

        public double Y { get; } = y;

        public string? Tooltip { get; } = tooltip;

        public string Glyph { get; } = glyph;

        public IBrush Foreground { get; } = foreground;

        public Bitmap? Image { get; init; }

        public bool HasImage => Image is not null;

        public bool ShowGlyphFallback => Image is null;
    }

    public sealed partial class MinimapViewModel : ViewModelBase, INavigationAware
    {
        private static readonly ConcurrentDictionary<string, Bitmap> BitmapCache = new();

        private readonly IMapSectionService _mapSectionService;
        private readonly IMinimapTileCatalog _tileCatalog;
        private readonly IMonsterSpawnQueryService _monsterSpawnQueryService;
        private readonly IMonsterImageCatalogService _monsterImageCatalogService;
        private readonly IErrorHandlingService _errorHandling;
        private readonly UserPreferencesService? _userPreferencesService;
        
        private bool _isInitializing;
        
        private const int TileSize = 256;
        private const int PrefetchTiles = 1;
        private const int PrefetchMargin = TileSize * PrefetchTiles;
        private const double MarkerScreenSize = 18.0;
        private const double SpawnMarkerScreenSize = MarkerScreenSize * 1.2;
        private const double MinZoom = 0.5;
        private const double MaxZoom = 4.0;
        private const int SpawnSuggestionLimit = 12;
        private const int SpawnSearchMaxLength = 64;
        private const int DefaultSpawnRenderLimit = 600;
        private const int ZoomedOutSpawnRenderLimit = 180;

        private bool _isReloading;
        private bool _pendingReload;
        private CancellationTokenSource? _debouncedReloadCts;
        private CancellationTokenSource? _reloadCts;
        private CancellationTokenSource? _spawnOverlayCts;
        private CancellationTokenSource? _spawnPreloadCts;
        private CancellationTokenSource? _spawnSearchCts;
        private bool _monsterImageCatalogInitialized;
        private bool _monsterImageCatalogWarmupStarted;
        
        private readonly IMinimapMarkerService _markerService;

        private static readonly IReadOnlyDictionary<byte, MarkerVisual> MarkerVisualByIconId = new Dictionary<byte, MarkerVisual>
        {
            [0x00] = new MarkerVisual("●", Brushes.White, "Marker (white)"),
            [0x01] = new MarkerVisual("◆", Brushes.LimeGreen, "Marker (green)"),
            [0x02] = new MarkerVisual("■", Brushes.Orange, "Marker (orange)"),
            [0x03] = new MarkerVisual("●", Brushes.Red, "Marker (red)"),
            [0x04] = new MarkerVisual("★", Brushes.Gold, "Marker (star)"),
            [0x05] = new MarkerVisual("✚", Brushes.DeepSkyBlue, "Marker (cross)"),
            [0x06] = new MarkerVisual("⬢", Brushes.MediumPurple, "Marker (hex)"),
            [0x07] = new MarkerVisual("◆", Brushes.Sienna, "Marker (brown)"),
            [0x08] = new MarkerVisual("✖", Brushes.LightGray, "Marker (x)"),
            [0x09] = new MarkerVisual("⚑", Brushes.DodgerBlue, "Flag"),
            [0x0A] = new MarkerVisual("?", Brushes.Gold, "Question"),
            [0x0B] = new MarkerVisual("!", Brushes.OrangeRed, "Important"),
            [0x0C] = new MarkerVisual("⌂", Brushes.Wheat, "House"),
            [0x0D] = new MarkerVisual("$", Brushes.LimeGreen, "Money"),
            [0x0E] = new MarkerVisual("⬆", Brushes.Red, "Up (red)"),
            [0x0F] = new MarkerVisual("⬇", Brushes.Red, "Down (red)"),
            [0x10] = new MarkerVisual("➡", Brushes.Red, "Right (red)"),
            [0x11] = new MarkerVisual("⬅", Brushes.Red, "Left (red)"),
            [0x12] = new MarkerVisual("⬆", Brushes.LimeGreen, "Up"),
            [0x13] = new MarkerVisual("⬇", Brushes.LimeGreen, "Down")
        };

        private readonly record struct MarkerVisual(string Glyph, IBrush Foreground, string FallbackText);

        public ObservableCollection<MinimapMarkerViewModel> Markers { get; } = new();
        public ObservableCollection<MinimapMarkerViewModel> SpawnMarkers { get; } = new();
        
        [ObservableProperty] private Character? _activeCharacter;

        [ObservableProperty] private int _centerX;
        [ObservableProperty] private int _centerY;
        [ObservableProperty] private byte _z;
        
        [ObservableProperty] private int _viewWidth = 1024;
        [ObservableProperty] private int _viewHeight = 768;
        
        [ObservableProperty] private int _viewportWidth;
        [ObservableProperty] private int _viewportHeight;
        
        [ObservableProperty] private double _panX;
        [ObservableProperty] private double _panY;
        [ObservableProperty] private double _zoom = 1.0;
        [ObservableProperty] private int _cursorWorldX;
        [ObservableProperty] private int _cursorWorldY;
        [ObservableProperty] private byte _cursorWorldZ;
        [ObservableProperty] private string _jumpCoordinatesInput = string.Empty;
        [ObservableProperty] private bool _showMarkers = true;
        [ObservableProperty] private bool _showSpawns = true;
        [ObservableProperty] private string _spawnSearchInput = string.Empty;
        [ObservableProperty] private string _activeSpawnFilter = string.Empty;
        [ObservableProperty] private string? _selectedSpawnSuggestion;
        
        [ObservableProperty] private string _statusText = string.Empty;

        public int SectionWidth => ComputeSectionSize(ViewportWidth);
        public int SectionHeight => ComputeSectionSize(ViewportHeight);

        public double RenderOffsetX => PanX + (ViewportWidth / 2.0) - (SectionWidth / 2.0);
        public double RenderOffsetY => PanY + (ViewportHeight / 2.0) - (SectionHeight / 2.0);
        public double ZoomCompensationX => (ViewportWidth * (1.0 - Zoom)) / 2.0;
        public double ZoomCompensationY => (ViewportHeight * (1.0 - Zoom)) / 2.0;
        public double MarkerScale => Zoom > 0.001 ? (1.0 / Zoom) : 1.0;
        public double MarkerScreenPixelSize => MarkerScreenSize;
        public double MarkerTranslationWorld => -(MarkerScreenSize / (2.0 * (Zoom > 0.001 ? Zoom : 1.0)));
        public double SpawnMarkerScreenPixelSize => SpawnMarkerScreenSize;
        public double SpawnMarkerTranslationWorld => -(SpawnMarkerScreenSize / (2.0 * (Zoom > 0.001 ? Zoom : 1.0)));
        public string CursorCoordinatesText =>
            $"{FormatTibiaCoordinate(CursorWorldX)},{FormatTibiaCoordinate(CursorWorldY)},{CursorWorldZ}";
        public bool HasActiveSpawnFilter => !string.IsNullOrWhiteSpace(ActiveSpawnFilter);
        public string SpawnFilterDisplayText => HasActiveSpawnFilter
            ? $"Spawn filter: {ActiveSpawnFilter}"
            : "Spawn filter: all monsters";

        public ObservableCollection<byte> AvailableZLevels { get; } = [];
        public ObservableCollection<MinimapTileViewModel> Tiles { get; } = [];
        public ObservableCollection<string> SpawnNameSuggestions { get; } = [];

        public MinimapViewModel(
            IMapSectionService mapSectionService,
            IMinimapTileCatalog tileCatalog,
            IMinimapMarkerService markerService,
            IMonsterSpawnQueryService monsterSpawnQueryService,
            IMonsterImageCatalogService monsterImageCatalogService,
            IErrorHandlingService errorHandling,
            UserPreferencesService? userPreferencesService = null)
        {
            _mapSectionService = mapSectionService;
            _tileCatalog = tileCatalog;
            _markerService = markerService;
            _monsterSpawnQueryService = monsterSpawnQueryService;
            _monsterImageCatalogService = monsterImageCatalogService;
            _errorHandling = errorHandling;
            _userPreferencesService = userPreferencesService;

            if (_userPreferencesService is not null)
            {
                (bool showMarkers, bool showSpawns) = _userPreferencesService.GetMinimapVisibilityPreferences();
                _showMarkers = showMarkers;
                _showSpawns = showSpawns;
            }

            CenterX = 32000;
            CenterY = 32000;
            Z = 7;
            CursorWorldX = CenterX;
            CursorWorldY = CenterY;
            CursorWorldZ = Z;
        }
        
        public void OnNavigatedTo(object? parameter)
        {
            if (parameter is NavigationParameters.MinimapWithTarget target)
            {
                ActiveCharacter = target.Character;
                CenterX = target.X;
                CenterY = target.Y;
                Z = target.Z;
                PanX = 0;
                PanY = 0;
                CursorWorldX = CenterX;
                CursorWorldY = CenterY;
                CursorWorldZ = Z;
                OnPropertyChanged(nameof(CursorCoordinatesText));

                _ = ReloadWithGateAsync();
                return;
            }

            if (parameter is NavigationParameters.MinimapWithCharacter withCharacter)
            {
                ActiveCharacter = withCharacter.Character;
            }

            InitializeZLevelsAndDefaultCenter();
            _ = ReloadWithGateAsync();
        }
        
        public void OnNavigatedFrom()
        {
            CancelAndDispose(ref _debouncedReloadCts);
            CancelAndDispose(ref _reloadCts);
            CancelAndDispose(ref _spawnOverlayCts);
            CancelAndDispose(ref _spawnPreloadCts);
            CancelAndDispose(ref _spawnSearchCts);
        }
        
        partial void OnZChanged(byte value)
        {
            CursorWorldZ = value;
            OnPropertyChanged(nameof(CursorCoordinatesText));

            if (_isInitializing || AvailableZLevels.Count == 0)
            {
                return;
            }

            _ = ReloadAsync();
        }
        
        private void InitializeZLevelsAndDefaultCenter()
        {
            _isInitializing = true;

            AvailableZLevels.Clear();

            for (byte level = 0; level <= 15; level++)
            {
                Core.Map.Map.MapBounds bounds = _tileCatalog.GetKnownBounds(level);
                bool hasBounds = bounds.MinX != 0 || bounds.MinY != 0 || bounds.MaxX != 0 || bounds.MaxY != 0;

                if (hasBounds)
                {
                    AvailableZLevels.Add(level);
                }
            }

            if (AvailableZLevels.Count == 0)
            {
                Z = 7;
                CenterX = 32000;
                CenterY = 32000;
                StatusText = "No minimap tiles found.";
                _isInitializing = false;
                return;
            }

            if (!AvailableZLevels.Contains(Z))
            {
                Z = AvailableZLevels[0];
            }

            Core.Map.Map.MapBounds zBounds = _tileCatalog.GetKnownBounds(Z);
            int midX = zBounds.MinX + ((zBounds.MaxX - zBounds.MinX) / 2);
            int midY = zBounds.MinY + ((zBounds.MaxY - zBounds.MinY) / 2);

            if (CenterX == 32000 && CenterY == 32000)
            {
                CenterX = midX;
                CenterY = midY;
            }

            CursorWorldX = CenterX;
            CursorWorldY = CenterY;
            CursorWorldZ = Z;
            OnPropertyChanged(nameof(CursorCoordinatesText));

            _isInitializing = false;
        }
        
        public async Task UpdateViewportSizeAsync(int width, int height)
        {
            if (width < 64 || height < 64)
            {
                return;
            }

            if (width == ViewportWidth && height == ViewportHeight)
            {
                return;
            }

            ViewportWidth = width;
            ViewportHeight = height;
            ClampViewportToKnownBounds();

            await ReloadWithGateAsync();
        }
        
        public async Task CommitPanAsync()
        {
            if (Math.Abs(PanX) >= 0.5 || Math.Abs(PanY) >= 0.5)
            {
                CenterX = CenterX - (int)Math.Round(PanX);
                CenterY = CenterY - (int)Math.Round(PanY);

                PanX = 0;
                PanY = 0;
            }

            ClampViewportToKnownBounds();

            await ReloadWithGateAsync();
        }

        public void ApplyWheelZoom(double wheelDelta, double anchorX, double anchorY)
        {
            if (Math.Abs(wheelDelta) < 0.001)
            {
                return;
            }

            double oldZoom = Zoom;
            double targetZoom = oldZoom * Math.Pow(1.2, wheelDelta);
            double newZoom = Math.Clamp(targetZoom, MinZoom, MaxZoom);

            if (Math.Abs(newZoom - oldZoom) < 0.0001)
            {
                return;
            }

            // Keep the map point under the cursor stable while zooming.
            double anchorDx = anchorX - (ViewportWidth / 2.0);
            double anchorDy = anchorY - (ViewportHeight / 2.0);

            PanX = PanX + (anchorDx * ((1.0 / newZoom) - (1.0 / oldZoom)));
            PanY = PanY + (anchorDy * ((1.0 / newZoom) - (1.0 / oldZoom)));
            Zoom = newZoom;

            bool needsReload = NormalizePanAndShiftCenterByTiles();
            ClampViewportToKnownBounds();
            // Keep zoom interaction responsive: render transform updates immediately,
            // tile loading is delayed until wheel input settles.
            RequestReloadDebounced(needsReload ? 35 : 90);
        }
        
        partial void OnViewportWidthChanged(int value)
        {
            OnPropertyChanged(nameof(SectionWidth));
            OnPropertyChanged(nameof(ZoomCompensationX));
            OnPropertyChanged(nameof(RenderOffsetX));
        }

        partial void OnViewportHeightChanged(int value)
        {
            OnPropertyChanged(nameof(SectionHeight));
            OnPropertyChanged(nameof(ZoomCompensationY));
            OnPropertyChanged(nameof(RenderOffsetY));
        }

        partial void OnZoomChanged(double value)
        {
            OnPropertyChanged(nameof(SectionWidth));
            OnPropertyChanged(nameof(SectionHeight));
            OnPropertyChanged(nameof(ZoomCompensationX));
            OnPropertyChanged(nameof(ZoomCompensationY));
            OnPropertyChanged(nameof(RenderOffsetX));
            OnPropertyChanged(nameof(RenderOffsetY));
            OnPropertyChanged(nameof(MarkerScale));
            OnPropertyChanged(nameof(MarkerTranslationWorld));
            OnPropertyChanged(nameof(SpawnMarkerTranslationWorld));
        }

        partial void OnShowMarkersChanged(bool value)
        {
            _userPreferencesService?.SaveMinimapVisibilityPreferences(ShowMarkers, ShowSpawns);

            if (!value)
            {
                Markers.Clear();
                return;
            }

            RequestReload();
        }

        partial void OnShowSpawnsChanged(bool value)
        {
            _userPreferencesService?.SaveMinimapVisibilityPreferences(ShowMarkers, ShowSpawns);

            if (!value)
            {
                CancelAndDispose(ref _spawnOverlayCts);
                CancelAndDispose(ref _spawnPreloadCts);
                SpawnMarkers.Clear();
                return;
            }

            RequestReload();
        }

        partial void OnSpawnSearchInputChanged(string value)
        {
            QueueSpawnSuggestions(value);
        }

        partial void OnSelectedSpawnSuggestionChanged(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            SpawnSearchInput = value.Trim();
        }

        partial void OnActiveSpawnFilterChanged(string value)
        {
            OnPropertyChanged(nameof(HasActiveSpawnFilter));
            OnPropertyChanged(nameof(SpawnFilterDisplayText));
        }

        partial void OnPanXChanged(double value)
        {
            OnPropertyChanged(nameof(RenderOffsetX));
        }

        partial void OnPanYChanged(double value)
        {
            OnPropertyChanged(nameof(RenderOffsetY));
        }
        
        public void ApplyDragDelta(double deltaX, double deltaY)
        {
            if (Math.Abs(deltaX) < 0.01 && Math.Abs(deltaY) < 0.01)
            {
                return;
            }

            PanX = PanX + deltaX;
            PanY = PanY + deltaY;

            bool needsReload = NormalizePanAndShiftCenterByTiles();
            ClampViewportToKnownBounds();
            if (needsReload)
            {
                RequestReload();
            }
        }

        public void UpdateCursorFromViewport(double viewportX, double viewportY)
        {
            if (ViewportWidth <= 0 || ViewportHeight <= 0)
            {
                return;
            }

            double zoom = Zoom > 0.001 ? Zoom : 1.0;

            // Converts viewport pixels back to tibia world coordinates.
            double worldX = CenterX + ((viewportX - (ViewportWidth / 2.0)) / zoom) - PanX;
            double worldY = CenterY + ((viewportY - (ViewportHeight / 2.0)) / zoom) - PanY;

            CursorWorldX = (int)Math.Round(worldX);
            CursorWorldY = (int)Math.Round(worldY);
            CursorWorldZ = Z;
            OnPropertyChanged(nameof(CursorCoordinatesText));
        }
        
        private bool NormalizePanAndShiftCenterByTiles()
        {
            bool changed = false;

            int xTileShift = (int)(PanX / TileSize);
            if (xTileShift != 0)
            {
                int shift = xTileShift * TileSize;
                CenterX = CenterX - shift;
                PanX = PanX - shift;
                changed = true;
            }

            int yTileShift = (int)(PanY / TileSize);
            if (yTileShift != 0)
            {
                int shift = yTileShift * TileSize;
                CenterY = CenterY - shift;
                PanY = PanY - shift;
                changed = true;
            }

            return changed;
        }

        private static void TryCancel(CancellationTokenSource? source)
        {
            if (source is null)
            {
                return;
            }

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Another path already disposed the source.
            }
        }

        private static void CancelAndDispose(ref CancellationTokenSource? source)
        {
            CancellationTokenSource? current = Interlocked.Exchange(ref source, null);
            if (current is null)
            {
                return;
            }

            TryCancel(current);
            current.Dispose();
        }
        
        private void RequestReload()
        {
            _ = ReloadWithGateAsync();
        }

        private void RequestReloadDebounced(int delayMs)
        {
            if (delayMs < 0)
            {
                delayMs = 0;
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationTokenSource? previous = Interlocked.Exchange(ref _debouncedReloadCts, cts);
            TryCancel(previous);
            previous?.Dispose();

            _ = RequestReloadDebouncedAsync(delayMs, cts.Token);
        }

        private async Task RequestReloadDebouncedAsync(int delayMs, CancellationToken token)
        {
            try
            {
                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, token);
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                await ReloadWithGateAsync();
            }
            catch (OperationCanceledException)
            {
                // Ignored: superseded by newer zoom interaction.
            }
        }

        private async Task ReloadWithGateAsync()
        {
            if (_isReloading)
            {
                _pendingReload = true;
                TryCancel(_reloadCts);
                return;
            }

            _isReloading = true;

            try
            {
                do
                {
                    _pendingReload = false;
                    await ReloadNowAsync();
                }
                while (_pendingReload);
            }
            finally
            {
                _isReloading = false;
            }
        }

        private async Task ReloadNowAsync()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationTokenSource? previous = Interlocked.Exchange(ref _reloadCts, cts);
            TryCancel(previous);
            previous?.Dispose();
            CancellationToken reloadToken = cts.Token;

            try
            {
                ClampViewportToKnownBounds();

                int viewportWidth = (ViewportWidth > 0) ? ViewportWidth : 1024;
                int viewportHeight = (ViewportHeight > 0) ? ViewportHeight : 768;

                int sectionWidth = ComputeSectionSize(viewportWidth);
                int sectionHeight = ComputeSectionSize(viewportHeight);

                Core.Map.Map.MapSectionRequest request = new Core.Map.Map.MapSectionRequest(
                    CenterX: CenterX,
                    CenterY: CenterY,
                    Z: Z,
                    Width: sectionWidth,
                    Height: sectionHeight,
                    Layer: Core.Map.Map.MinimapLayer.Color
                );

                Core.Map.Map.MapSection section = await Task.Run(() => _mapSectionService.GetSection(request), reloadToken);
                TileLoadResult tileLoad = await LoadTilesAsync(section, reloadToken);

                if (reloadToken.IsCancellationRequested)
                {
                    return;
                }

                Tiles.Clear();
                foreach (MinimapTileViewModel tile in tileLoad.Tiles)
                {
                    Tiles.Add(tile);
                }

                double minX = tileLoad.MinX;
                double minY = tileLoad.MinY;
                double maxX = tileLoad.MaxX;
                double maxY = tileLoad.MaxY;

                // Tiles sind 256x256
                double contentLeft = minX + RenderOffsetX;
                double contentTop = minY + RenderOffsetY;
                double contentRight = (maxX + 256) + RenderOffsetX;
                double contentBottom = (maxY + 256) + RenderOffsetY;

                contentLeft *= Zoom;
                contentTop *= Zoom;
                contentRight *= Zoom;
                contentBottom *= Zoom;

                bool anyVisible =
                !(contentRight < 0 ||
                  contentBottom < 0 ||
                  contentLeft > ViewportWidth ||
                  contentTop > ViewportHeight);

                StatusText =
                $"Center: {CenterX}/{CenterY} Z:{Z} Tiles:{Tiles.Count} " +
                $"VP:{ViewportWidth}x{ViewportHeight} Sec:{SectionWidth}x{SectionHeight} " +
                $"Off:{RenderOffsetX:0.#},{RenderOffsetY:0.#} Zoom:{Zoom:0.00} " +
                $"TileBoundsX:{minX:0.#}..{maxX:0.#} TileBoundsY:{minY:0.#}..{maxY:0.#} " +
                $"Vis:{anyVisible} Load:{tileLoad.LoadedCount}/{tileLoad.TotalCount}";
                
                UpdateMarkers(section);
                StartSpawnOverlayUpdate(section);
            }
            catch (OperationCanceledException)
            {
                // Expected during fast panning/zooming; newer request supersedes this one.
            }
            catch (Exception ex)
            {
                await _errorHandling.HandleExceptionAsync(ex, userMessage: "Failed to load minimap tiles", context: "MinimapViewModel.ReloadNowAsync");
                StatusText = $"Failed to load minimap: {ex.GetType().Name}: {ex.Message}";
            }
        }
        
        private void UpdateMarkers(Core.Map.Map.MapSection section)
        {
            Markers.Clear();
            if (!ShowMarkers)
            {
                return;
            }

            int minX = section.OriginX;
            int minY = section.OriginY;
            int maxX = section.OriginX + section.Width - 1;
            int maxY = section.OriginY + section.Height - 1;

            IReadOnlyList<Core.Map.Map.MinimapMarker> visible = _markerService.GetMarkersInBounds(
                minX: minX,
                minY: minY,
                maxX: maxX,
                maxY: maxY,
                z: section.Z);

            int i = 0;
            while (i < visible.Count)
            {
                Core.Map.Map.MinimapMarker m = visible[i];
                i += 1;

                double x = m.X - section.OriginX;
                double y = m.Y - section.OriginY;

                (string glyph, IBrush brush, string fallback) = GetMarkerVisual(m.IconId);

                string? tip = !string.IsNullOrWhiteSpace(m.Text) ? m.Text : null;

                Markers.Add(new MinimapMarkerViewModel(x, y, tip, glyph, brush));
            }
        }
        private static (string Glyph, IBrush Brush, string FallbackText) GetMarkerVisual(byte iconId)
        {
            if (MarkerVisualByIconId.TryGetValue(iconId, out MarkerVisual visual))
            {
                return (visual.Glyph, visual.Foreground, visual.FallbackText);
            }

            return ("•", Brushes.White, $"Marker (icon {iconId})");
        }

        private void StartSpawnOverlayUpdate(Core.Map.Map.MapSection section)
        {
            CancellationTokenSource cts = new();
            CancellationTokenSource? previous = Interlocked.Exchange(ref _spawnOverlayCts, cts);
            TryCancel(previous);
            previous?.Dispose();

            _ = UpdateSpawnMarkersAsync(section, cts.Token);
        }

        private async Task UpdateSpawnMarkersAsync(Core.Map.Map.MapSection section, CancellationToken token)
        {
            try
            {
                if (!ShowSpawns)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => SpawnMarkers.Clear(), DispatcherPriority.Background);
                    return;
                }

                EnsureMonsterImageCatalogWarmup();

                int spawnLimit = ComputeSpawnRenderLimit();
                string? monsterNameFilter = string.IsNullOrWhiteSpace(ActiveSpawnFilter) ? null : ActiveSpawnFilter.Trim();
                WorldBounds visibleBounds = GetVisibleWorldBounds(section);

                IReadOnlyList<Core.Map.Map.MonsterSpawnMarker> visibleSpawns = await _monsterSpawnQueryService.GetSpawnsInBoundsAsync(
                    minX: visibleBounds.MinX,
                    minY: visibleBounds.MinY,
                    maxX: visibleBounds.MaxX,
                    maxY: visibleBounds.MaxY,
                    z: section.Z,
                    monsterName: monsterNameFilter,
                    maxResults: spawnLimit,
                    ct: token);

                List<MinimapMarkerViewModel> mapped = await Task.Run(() =>
                {
                    List<MinimapMarkerViewModel> items = new(visibleSpawns.Count);
                    int index = 0;
                    while (index < visibleSpawns.Count)
                    {
                        token.ThrowIfCancellationRequested();

                        Core.Map.Map.MonsterSpawnMarker spawn = visibleSpawns[index];
                        index += 1;

                        double x = spawn.X - section.OriginX;
                        double y = spawn.Y - section.OriginY;

                        string tooltip = spawn.SpawnTimeSeconds.HasValue
                            ? $"{spawn.MonsterName} ({spawn.SpawnTimeSeconds.Value}s)"
                            : spawn.MonsterName;

                        Bitmap? image = TryResolveMonsterBitmap(spawn);

                        items.Add(new MinimapMarkerViewModel(
                            x,
                            y,
                            tooltip,
                            "●",
                            Brushes.Orange)
                        {
                            Image = image
                        });
                    }

                    return items;
                }, token);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    SpawnMarkers.Clear();
                    foreach (MinimapMarkerViewModel marker in mapped)
                    {
                        SpawnMarkers.Add(marker);
                    }
                }, DispatcherPriority.Background);

                StartSpawnPreload(section);
            }
            catch (OperationCanceledException)
            {
                // Ignored: superseded by newer viewport request.
            }
            catch (Exception ex)
            {
                // Spawn overlay failures should not break map interaction.
                _ = Dispatcher.UIThread.InvokeAsync(
                    () => StatusText = $"Spawn overlay error: {ex.GetType().Name}",
                    DispatcherPriority.Background);
            }
        }

        private Bitmap? TryResolveMonsterBitmap(Core.Map.Map.MonsterSpawnMarker spawn)
        {
            if (!_monsterImageCatalogService.TryResolveImageUri(spawn.CreatureId, spawn.MonsterName, out string imageUri))
            {
                return null;
            }

            if (BitmapCache.TryGetValue(imageUri, out Bitmap? cached))
            {
                return cached;
            }

            if (!TryLoadBitmap(imageUri, out Bitmap bitmap, out _))
            {
                return null;
            }

            BitmapCache[imageUri] = bitmap;
            return bitmap;
        }

        private void StartSpawnPreload(Core.Map.Map.MapSection section)
        {
            CancellationTokenSource cts = new();
            CancellationTokenSource? previous = Interlocked.Exchange(ref _spawnPreloadCts, cts);
            TryCancel(previous);
            previous?.Dispose();

            _ = PreloadNearbySpawnImagesAsync(section, cts.Token);
        }

        private async Task PreloadNearbySpawnImagesAsync(Core.Map.Map.MapSection section, CancellationToken token)
        {
            try
            {
                int extraX = Math.Max(section.Width / 2, 256);
                int extraY = Math.Max(section.Height / 2, 256);

                int minX = section.OriginX - extraX;
                int minY = section.OriginY - extraY;
                int maxX = section.OriginX + section.Width - 1 + extraX;
                int maxY = section.OriginY + section.Height - 1 + extraY;
                string? monsterNameFilter = string.IsNullOrWhiteSpace(ActiveSpawnFilter) ? null : ActiveSpawnFilter.Trim();
                int preloadLimit = ComputeSpawnRenderLimit() * 2;

                IReadOnlyList<Core.Map.Map.MonsterSpawnMarker> nearby = await _monsterSpawnQueryService.GetSpawnsInBoundsAsync(
                    minX: minX,
                    minY: minY,
                    maxX: maxX,
                    maxY: maxY,
                    z: section.Z,
                    monsterName: monsterNameFilter,
                    maxResults: preloadLimit,
                    ct: token);

                int i = 0;
                while (i < nearby.Count)
                {
                    token.ThrowIfCancellationRequested();
                    TryResolveMonsterBitmap(nearby[i]);
                    i += 1;
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored: superseded by newer viewport request.
            }
            catch
            {
                // Background preload should not disrupt the interactive map.
            }
        }

        private void EnsureMonsterImageCatalogWarmup()
        {
            if (_monsterImageCatalogInitialized || _monsterImageCatalogWarmupStarted)
            {
                return;
            }

            _monsterImageCatalogWarmupStarted = true;
            _ = Task.Run(async () =>
            {
                try
                {
                    await _monsterImageCatalogService.EnsureCatalogAsync();
                    _monsterImageCatalogInitialized = true;

                    await Dispatcher.UIThread.InvokeAsync(() => RequestReload(), DispatcherPriority.Background);
                }
                catch
                {
                    _monsterImageCatalogInitialized = false;
                }
                finally
                {
                    _monsterImageCatalogWarmupStarted = false;
                }
            });
        }

        private int ComputeSpawnRenderLimit()
        {
            return Zoom <= 0.8 ? ZoomedOutSpawnRenderLimit : DefaultSpawnRenderLimit;
        }

        private WorldBounds GetVisibleWorldBounds(Core.Map.Map.MapSection section)
        {
            double zoom = Zoom > 0.001 ? Zoom : 1.0;
            int vpWidth = ViewportWidth > 0 ? ViewportWidth : 1024;
            int vpHeight = ViewportHeight > 0 ? ViewportHeight : 768;

            // Screen center in world coordinates after pan.
            double screenCenterX = CenterX - PanX;
            double screenCenterY = CenterY - PanY;
            double halfVisibleX = vpWidth / (2.0 * zoom);
            double halfVisibleY = vpHeight / (2.0 * zoom);

            int minX = (int)Math.Floor(screenCenterX - halfVisibleX);
            int maxX = (int)Math.Ceiling(screenCenterX + halfVisibleX);
            int minY = (int)Math.Floor(screenCenterY - halfVisibleY);
            int maxY = (int)Math.Ceiling(screenCenterY + halfVisibleY);

            // Keep query inside currently loaded section bounds.
            int sectionMinX = section.OriginX;
            int sectionMinY = section.OriginY;
            int sectionMaxX = section.OriginX + section.Width - 1;
            int sectionMaxY = section.OriginY + section.Height - 1;

            minX = Math.Max(minX, sectionMinX);
            minY = Math.Max(minY, sectionMinY);
            maxX = Math.Min(maxX, sectionMaxX);
            maxY = Math.Min(maxY, sectionMaxY);

            return new WorldBounds(minX, minY, maxX, maxY);
        }

        private void QueueSpawnSuggestions(string rawInput)
        {
            CancelAndDispose(ref _spawnSearchCts);

            if (string.IsNullOrWhiteSpace(rawInput))
            {
                SpawnNameSuggestions.Clear();
                return;
            }

            string query = rawInput.Trim();
            if (query.Length > SpawnSearchMaxLength)
            {
                query = query[..SpawnSearchMaxLength];
            }

            CancellationTokenSource cts = new();
            Interlocked.Exchange(ref _spawnSearchCts, cts);
            _ = LoadSpawnSuggestionsAsync(query, cts.Token);
        }

        private async Task LoadSpawnSuggestionsAsync(string query, CancellationToken token)
        {
            try
            {
                await Task.Delay(120, token);

                IReadOnlyList<string> suggestions = await _monsterSpawnQueryService.SearchMonsterNamesAsync(
                    query,
                    limit: SpawnSuggestionLimit,
                    ct: token);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    SpawnNameSuggestions.Clear();
                    foreach (string suggestion in suggestions)
                    {
                        SpawnNameSuggestions.Add(suggestion);
                    }
                }, DispatcherPriority.Background);
            }
            catch (OperationCanceledException)
            {
                // Expected when user keeps typing.
            }
            catch
            {
                // Suggestions are non-critical.
            }
        }

        [RelayCommand]
        private void ApplySpawnFilter()
        {
            string filter = (SpawnSearchInput ?? string.Empty).Trim();
            if (filter.Length > SpawnSearchMaxLength)
            {
                filter = filter[..SpawnSearchMaxLength];
                SpawnSearchInput = filter;
            }

            bool changed = !string.Equals(ActiveSpawnFilter, filter, StringComparison.Ordinal);
            ActiveSpawnFilter = filter;

            if (!string.IsNullOrWhiteSpace(filter))
            {
                SelectedSpawnSuggestion = filter;
            }

            if (changed)
            {
                RequestReload();
            }
        }

        [RelayCommand]
        private void ClearSpawnFilter()
        {
            bool changed = !string.IsNullOrWhiteSpace(SpawnSearchInput) ||
                           !string.IsNullOrWhiteSpace(ActiveSpawnFilter) ||
                           !string.IsNullOrWhiteSpace(SelectedSpawnSuggestion);

            SpawnSearchInput = string.Empty;
            ActiveSpawnFilter = string.Empty;
            SelectedSpawnSuggestion = null;
            SpawnNameSuggestions.Clear();

            if (changed)
            {
                RequestReload();
            }
        }

        [RelayCommand]
        private async Task ReloadAsync()
        {
            await ReloadWithGateAsync();
        }

        [RelayCommand]
        private async Task JumpToCoordinatesAsync()
        {
            if (!TryParseJumpCoordinates(JumpCoordinatesInput, out int x, out int y, out byte z, out string error))
            {
                StatusText = error;
                return;
            }

            CenterX = x;
            CenterY = y;
            Z = z;
            PanX = 0;
            PanY = 0;
            Zoom = 1.0;
            ClampViewportToKnownBounds();
            CursorWorldX = x;
            CursorWorldY = y;
            CursorWorldZ = z;
            OnPropertyChanged(nameof(CursorCoordinatesText));

            await ReloadWithGateAsync();
        }

        [RelayCommand]
        private async Task ResetViewAsync()
        {
            // Reset to default center position (Thais temple)
            CenterX = 32369;
            CenterY = 32241;
            Z = 7;

            // Reset zoom and pan
            Zoom = 1.0;
            PanX = 0;
            PanY = 0;

            ClampViewportToKnownBounds();

            // Update cursor
            CursorWorldX = CenterX;
            CursorWorldY = CenterY;
            CursorWorldZ = Z;
            OnPropertyChanged(nameof(CursorCoordinatesText));

            StatusText = "View reset to default position";
            await ReloadWithGateAsync();
        }
        
        [RelayCommand]
        public async Task ZUpAsync()
        {
            if (Z == 0)
            {
                return;
            }

            Z = (byte)(Z - 1);
            await ReloadAsync();
        }

        [RelayCommand]
        public async Task ZDownAsync()
        {
            if (Z == 15)
            {
                return;
            }

            Z = (byte)(Z + 1);
            await ReloadAsync();
        }

        private async Task<TileLoadResult> LoadTilesAsync(Core.Map.Map.MapSection section, CancellationToken token)
        {
            return await Task.Run(() =>
            {
                List<MinimapTileViewModel> items = new(section.Tiles.Count);
                int loaded = 0;
                int failed = 0;

                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                foreach (Core.Map.Map.MapSectionTile tile in section.Tiles)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        Bitmap bitmap = GetOrCreateCachedBitmap(tile.FilePath);
                        MinimapTileViewModel tileVm = new(
                            x: tile.OffsetX,
                            y: tile.OffsetY,
                            image: bitmap);

                        items.Add(tileVm);
                        loaded += 1;

                        if (tileVm.X < minX) minX = tileVm.X;
                        if (tileVm.Y < minY) minY = tileVm.Y;
                        if (tileVm.X > maxX) maxX = tileVm.X;
                        if (tileVm.Y > maxY) maxY = tileVm.Y;
                    }
                    catch
                    {
                        failed += 1;
                    }
                }

                if (items.Count == 0)
                {
                    minX = 0;
                    minY = 0;
                    maxX = 0;
                    maxY = 0;
                }

                return new TileLoadResult(
                    Tiles: items,
                    TotalCount: section.Tiles.Count,
                    LoadedCount: loaded,
                    FailedCount: failed,
                    MinX: minX,
                    MinY: minY,
                    MaxX: maxX,
                    MaxY: maxY);
            }, token);
        }

        private static bool TryLoadBitmap(string pathOrUri, out Bitmap bitmap, out string error)
        {
            bitmap = null!;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(pathOrUri))
            {
                error = "PathOrUri is null/empty.";
                return false;
            }

            try
            {
                // 1) Avalonia embedded resource
                if (pathOrUri.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
                {
                    using Stream stream = AssetLoader.Open(new Uri(pathOrUri));
                    bitmap = new Bitmap(stream);
                    return true;
                }

                // 2) Absolute URI (file://, etc.)
                if (Uri.TryCreate(pathOrUri, UriKind.Absolute, out Uri? uri))
                {
                    if (string.Equals(uri.Scheme, "file", StringComparison.OrdinalIgnoreCase))
                    {
                        using Stream stream = File.OpenRead(uri.LocalPath);
                        bitmap = new Bitmap(stream);
                        return true;
                    }

                    // Falls du später http(s) willst: hier ergänzen.
                    error = $"Unsupported URI scheme: {uri.Scheme}";
                    return false;
                }

                // 3) Fallback: treat as file system path (relative or absolute)
                using Stream fs = File.OpenRead(pathOrUri);
                bitmap = new Bitmap(fs);
                return true;
            }
            catch (Exception ex)
            {
                error = $"{ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }

        private static Bitmap GetOrCreateCachedBitmap(string pathOrUri)
        {
            if (BitmapCache.TryGetValue(pathOrUri, out Bitmap? cached))
            {
                return cached;
            }

            if (!TryLoadBitmap(pathOrUri, out Bitmap bmp, out string error))
            {
                // hier nicht throwen -> sonst killt ein kaputtes Tile die ganze Map
                throw new InvalidOperationException($"Failed to load bitmap '{pathOrUri}': {error}");
            }

            BitmapCache[pathOrUri] = bmp;
            return bmp;
        }

        private bool TryParseJumpCoordinates(string input, out int x, out int y, out byte z, out string error)
        {
            x = 0;
            y = 0;
            z = Z;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Please enter coordinates in format x,y,z (z optional).";
                return false;
            }

            if (UserInputSanitizer.ExceedsLength(input, UserInputLimits.CoordinateInputMaxLength))
            {
                error = $"Coordinate input is too long (max {UserInputLimits.CoordinateInputMaxLength} characters).";
                return false;
            }

            string[] parts = input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts.Length > 3)
            {
                error = "Invalid format. Use x,y,z (example: 129.195,128.16,7).";
                return false;
            }

            if (!TryParseCoordinate(parts[0], out x) || !TryParseCoordinate(parts[1], out y))
            {
                error = "Invalid X/Y coordinates. Example: 129.195,128.16,7.";
                return false;
            }

            if (parts.Length == 3 && !byte.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out z))
            {
                error = "Invalid Z level. Use values from 0 to 15.";
                return false;
            }

            if (z > 15)
            {
                error = "Invalid Z level. Use values from 0 to 15.";
                return false;
            }

            return true;
        }

        private static bool TryParseCoordinate(string token, out int value)
        {
            value = 0;

            string trimmed = token.Trim().Replace(" ", string.Empty);
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return false;
            }

            // TibiaWiki Mapper style:
            // 129.195 => (129 * 256) + 195
            if (trimmed.Contains('.') &&
                TibiaCoordinateConverter.TryParseExternalCoordinate(trimmed, out int parsed))
            {
                value = parsed;
                return true;
            }

            // Fallback: internal map coordinate, e.g. 33195
            return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static string FormatTibiaCoordinate(int value)
        {
            return TibiaCoordinateConverter.FormatExternalCoordinate(value);
        }

        private void ClampViewportToKnownBounds()
        {
            if (ViewportWidth <= 0 || ViewportHeight <= 0)
            {
                return;
            }

            Core.Map.Map.MapBounds bounds = _tileCatalog.GetKnownBounds(Z);
            bool hasBounds = bounds.MinX != 0 || bounds.MinY != 0 || bounds.MaxX != 0 || bounds.MaxY != 0;
            if (!hasBounds)
            {
                return;
            }

            double zoom = Zoom > 0.001 ? Zoom : 1.0;
            double clampedPanX = ClampPanForAxis(CenterX, PanX, ViewportWidth, zoom, bounds.MinX, bounds.MaxX);
            double clampedPanY = ClampPanForAxis(CenterY, PanY, ViewportHeight, zoom, bounds.MinY, bounds.MaxY);

            if (Math.Abs(clampedPanX - PanX) > 0.001)
            {
                PanX = clampedPanX;
            }

            if (Math.Abs(clampedPanY - PanY) > 0.001)
            {
                PanY = clampedPanY;
            }

            NormalizePanAndShiftCenterByTiles();
        }

        private static double ClampPanForAxis(int center, double pan, int viewport, double zoom, int min, int max)
        {
            double halfVisible = viewport / (2.0 * zoom);

            // centerScreenWorld = center - pan
            // min visible = centerScreenWorld - halfVisible >= min
            // max visible = centerScreenWorld + halfVisible <= max
            double minAllowedPan = center + halfVisible - max;
            double maxAllowedPan = center - halfVisible - min;

            if (minAllowedPan > maxAllowedPan)
            {
                double mapMid = min + ((max - min) / 2.0);
                return center - mapMid;
            }

            return Math.Clamp(pan, minAllowedPan, maxAllowedPan);
        }

        private int ComputeSectionSize(int viewport)
        {
            int vp = Math.Max(64, viewport);
            double zoom = Zoom > 0.001 ? Zoom : 1.0;
            int visibleWorldSize = (int)Math.Ceiling(vp / zoom);
            return visibleWorldSize + (PrefetchMargin * 2);
        }

        private sealed record TileLoadResult(
            IReadOnlyList<MinimapTileViewModel> Tiles,
            int TotalCount,
            int LoadedCount,
            int FailedCount,
            double MinX,
            double MinY,
            double MaxX,
            double MaxY);

        private readonly record struct WorldBounds(int MinX, int MinY, int MaxX, int MaxY);
    } 
}
