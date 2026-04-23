using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Map;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Infrastructure.Data;
using HuntingPlaceEntitys = TibiaHuntMaster.Infrastructure.Data.Entities.Content.HuntingPlaceEntity;

using static TibiaHuntMaster.App.Services.Navigation.NavigationParameters;

namespace TibiaHuntMaster.App.ViewModels.Dashboard
{
    public sealed partial class HuntingPlacesViewModel : ViewModelBase, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILocalizationService _localizationService;
        private readonly INavigationService _navigationService;
        private readonly Dictionary<string, HuntingPlaceCoordinate> _cityFallbackCoordinates = new(StringComparer.OrdinalIgnoreCase);
        private readonly bool _isDevMode;
        private readonly Dictionary<int, HuntingPlaceLocalStats> _localStatsByPlaceId = new();
        private Character? _activeCharacter;

        public HuntingPlacesViewModel(
            IDbContextFactory<AppDbContext> dbFactory,
            ILocalizationService localizationService,
            INavigationService navigationService)
        {
            _dbFactory = dbFactory;
            _localizationService = localizationService;
            _navigationService = navigationService;
            _isDevMode = IsDevModeEnabled();
            _statusMessage = _localizationService["HuntingPlaces_StatusLoading"];
            UpdateVocationFilterOptions();

            // Subscribe to language changes
            _localizationService.PropertyChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Refresh status message when language changes
            UpdateVocationFilterOptions();
            UpdateStatusMessage();

            // Refresh each place item's localized properties
            foreach (var item in Items)
            {
                item.RefreshLocalizedStrings();
            }
        }

        private void UpdateStatusMessage()
        {
            if (IsLoading)
            {
                StatusMessage = _localizationService["HuntingPlaces_StatusLoading"];
            }
            else if (Items.Count == 0)
            {
                StatusMessage = _localizationService["HuntingPlaces_StatusEmpty"];
            }
            else
            {
                StatusMessage = string.Format(_localizationService["HuntingPlaces_StatusCount"], Items.Count);
            }
        }
        private static List<HuntingPlaceEntitys>? _cachedPlaces;
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);
        private List<HuntingPlaceItemViewModel> _allPlaceViewModels = [];

        private string _currentVocation = string.Empty;
        [ObservableProperty]private bool _isLoading;

        [ObservableProperty]private ObservableCollection<HuntingPlaceItemViewModel> _items = [];
        [ObservableProperty]private int _maxLevel; // 0 = no limit
        [ObservableProperty]private int _minLevel;
        [ObservableProperty]private bool _onlyHuntedPlaces;
        [ObservableProperty]private bool _hasCharacterContext;
        [ObservableProperty]private ObservableCollection<string> _vocationFilterOptions = [];

        // Filter
        [ObservableProperty]private int _selectedVocationFilterIndex; // 0 = Character vocation
        [ObservableProperty]private string _statusMessage;
        [ObservableProperty]private bool _hasAggregatedStats;
        [ObservableProperty]private int _aggregatedSessionCount;
        [ObservableProperty]private string _aggregatedXpPerHour = "-";
        [ObservableProperty]private string _aggregatedLootPerHour = "-";
        [ObservableProperty]private string _aggregatedBalancePerHour = "-";

        public HuntingPlacesViewModel()
        {
            _dbFactory = null!;
            _localizationService = null!;
            _navigationService = null!;
            _isDevMode = false;
            _statusMessage = "Loading hunting places...";
            _vocationFilterOptions =
            [
                "Character Vocation",
                "Mages",
                "Knights",
                "Paladins"
            ];
        }

        partial void OnSelectedVocationFilterIndexChanged(int value)
        {
            ApplyFiltersImmediate();
        }
        partial void OnMinLevelChanged(int value)
        {
            ApplyFiltersImmediate();
        }
        partial void OnMaxLevelChanged(int value)
        {
            ApplyFiltersImmediate();
        }
        partial void OnOnlyHuntedPlacesChanged(bool value)
        {
            ApplyFiltersImmediate();
        }

        private void UpdateVocationFilterOptions()
        {
            VocationFilterOptions.Clear();
            VocationFilterOptions.Add(_localizationService["HuntingPlaces_FilterCharacterVocation"]);
            VocationFilterOptions.Add(_localizationService["HuntingPlaces_FilterMages"]);
            VocationFilterOptions.Add(_localizationService["HuntingPlaces_FilterKnights"]);
            VocationFilterOptions.Add(_localizationService["HuntingPlaces_FilterPaladins"]);

            if(SelectedVocationFilterIndex < 0 || SelectedVocationFilterIndex >= VocationFilterOptions.Count)
            {
                SelectedVocationFilterIndex = 0;
            }
        }

        public async Task LoadHuntingPlacesAsync(string characterVocation)
        {
            _currentVocation = characterVocation;

            // Debug: Log vocation
            Debug.WriteLine($"[HuntingPlaces] Loading for vocation: {characterVocation}");
            IsLoading = true;
            StatusMessage = _localizationService["HuntingPlaces_StatusLoading"];

            try
            {
                List<HuntingPlaceEntitys> places = await GetOrLoadPlacesAsync();
                Dictionary<int, HuntingPlaceLocalStats> localStats = await LoadLocalStatsByPlaceAsync();

                _localStatsByPlaceId.Clear();
                foreach(KeyValuePair<int, HuntingPlaceLocalStats> pair in localStats)
                {
                    _localStatsByPlaceId[pair.Key] = pair.Value;
                }

                _allPlaceViewModels = places
                                      .Select(place => new HuntingPlaceItemViewModel(
                                          place,
                                          _localizationService,
                                          _isDevMode,
                                          _localStatsByPlaceId.TryGetValue(place.Id, out HuntingPlaceLocalStats? stats) ? stats : null))
                                      .ToList();
                BuildCityFallbackCoordinates();
                ApplyFiltersImmediate();
            }
            finally
            {
                IsLoading = false;
                UpdateStatusMessage();
            }
        }

        private void ApplyFiltersImmediate()
        {
            if(_allPlaceViewModels.Count == 0)
            {
                Items.Clear();
                ClearAggregatedStats();
                StatusMessage = _localizationService["HuntingPlaces_StatusEmpty"];
                return;
            }

            // Determine vocation for filtering
            string filterVocation = SelectedVocationFilterIndex switch
            {
                1 => "Sorcerer/Druid",
                2 => "Knight",
                3 => "Paladin",
                _ => _currentVocation
            };

            // Debug
            Debug.WriteLine($"[HuntingPlaces] Filtering by vocation: {filterVocation} (Index: {SelectedVocationFilterIndex})");

            int minLvl = MinLevel;
            int maxLvl = MaxLevel;

            // Filter in-memory - super fast
            List<HuntingPlaceItemViewModel> filtered = _allPlaceViewModels.Where(vm =>
            {
                int? level = GetLevelForVocation(vm.Place, filterVocation);

                if(!level.HasValue)
                {
                    return false;
                }

                // Check min level
                if(minLvl > 0 && level.Value < minLvl)
                {
                    return false;
                }

                // Check max level (0 = no limit)
                if(maxLvl > 0 && level.Value > maxLvl)
                {
                    return false;
                }

                if(OnlyHuntedPlaces && !vm.HasLocalStats)
                {
                    return false;
                }

                return true;
            }).ToList();

            Items.Clear();
            foreach(HuntingPlaceItemViewModel vm in filtered)
            {
                Items.Add(vm);
            }

            UpdateAggregatedStats(filtered);
            StatusMessage = string.Format(_localizationService["HuntingPlaces_StatusCount"], Items.Count);
        }

        private async Task<List<HuntingPlaceEntitys>> GetOrLoadPlacesAsync()
        {
            if(_cachedPlaces != null)
            {
                return _cachedPlaces;
            }

            await _cacheLock.WaitAsync();
            try
            {
                if(_cachedPlaces == null)
                {
                    await using AppDbContext db = await _dbFactory.CreateDbContextAsync();

                    _cachedPlaces = await db.HuntingPlaces
                                            .AsNoTracking()
                                            .OrderBy(p => p.Name)
                                            .ToListAsync();
                }
            }
            finally
            {
                _cacheLock.Release();
            }

            return _cachedPlaces ?? [];
        }

        private async Task<Dictionary<int, HuntingPlaceLocalStats>> LoadLocalStatsByPlaceAsync()
        {
            if(_activeCharacter == null)
            {
                return new Dictionary<int, HuntingPlaceLocalStats>();
            }

            await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
            var sessions = await db.HuntSessions
                                   .AsNoTracking()
                                   .Where(s => s.CharacterId == _activeCharacter.Id && s.HuntingPlaceId.HasValue)
                                   .Select(s => new
                                   {
                                       HuntingPlaceId = s.HuntingPlaceId!.Value,
                                       s.Duration,
                                       s.XpPerHour,
                                       s.XpGain,
                                       s.Loot,
                                       s.Balance
                                   })
                                   .ToListAsync();

            Dictionary<int, LocalStatsAccumulator> accumulators = new();

            foreach(var session in sessions)
            {
                if(session.Duration <= TimeSpan.Zero)
                {
                    continue;
                }

                double hours = session.Duration.TotalHours;
                if(hours <= 0)
                {
                    continue;
                }

                if(!accumulators.TryGetValue(session.HuntingPlaceId, out LocalStatsAccumulator? accumulator))
                {
                    accumulator = new LocalStatsAccumulator();
                    accumulators[session.HuntingPlaceId] = accumulator;
                }

                double xpPerHour = session.XpPerHour > 0
                    ? session.XpPerHour
                    : session.XpGain > 0 ? session.XpGain / hours : 0d;

                accumulator.SessionCount++;
                accumulator.SumXpPerHour += xpPerHour;
                accumulator.SumLootPerHour += session.Loot / hours;
                accumulator.SumBalancePerHour += session.Balance / hours;
            }

            return accumulators
                  .Where(pair => pair.Value.SessionCount > 0)
                  .ToDictionary(
                      pair => pair.Key,
                      pair => pair.Value.ToStats());
        }

        private void UpdateAggregatedStats(IReadOnlyCollection<HuntingPlaceItemViewModel> filteredItems)
        {
            int totalSessions = 0;
            double totalXpPerHour = 0;
            double totalLootPerHour = 0;
            double totalBalancePerHour = 0;

            foreach(HuntingPlaceItemViewModel item in filteredItems)
            {
                if(!_localStatsByPlaceId.TryGetValue(item.Place.Id, out HuntingPlaceLocalStats? localStats))
                {
                    continue;
                }

                totalSessions += localStats.SessionCount;
                totalXpPerHour += localStats.SumXpPerHour;
                totalLootPerHour += localStats.SumLootPerHour;
                totalBalancePerHour += localStats.SumBalancePerHour;
            }

            if(totalSessions <= 0)
            {
                ClearAggregatedStats();
                return;
            }

            HasAggregatedStats = true;
            AggregatedSessionCount = totalSessions;
            AggregatedXpPerHour = (totalXpPerHour / totalSessions).ToString("N0");
            AggregatedLootPerHour = (totalLootPerHour / totalSessions).ToString("N0");
            AggregatedBalancePerHour = (totalBalancePerHour / totalSessions).ToString("N0");
        }

        private void ClearAggregatedStats()
        {
            HasAggregatedStats = false;
            AggregatedSessionCount = 0;
            AggregatedXpPerHour = "-";
            AggregatedLootPerHour = "-";
            AggregatedBalancePerHour = "-";
        }

        [RelayCommand]
        private void ShowOnMinimap(HuntingPlaceItemViewModel? place)
        {
            if(place == null || _activeCharacter == null)
            {
                return;
            }

            HuntingPlaceCoordinate? target = place.PrimaryCoordinate;
            if(target == null)
            {
                string cityKey = NormalizeCityKey(place.City);
                if(!string.IsNullOrWhiteSpace(cityKey) &&
                   _cityFallbackCoordinates.TryGetValue(cityKey, out HuntingPlaceCoordinate? cityFallback) &&
                   cityFallback != null)
                {
                    target = cityFallback;
                }
            }

            if(target == null)
            {
                StatusMessage = string.Format(_localizationService["HuntingPlaces_MapNoCoordinatesForPlace"], place.Name);
                return;
            }

            _navigationService.NavigateTo<MinimapViewModel>(new MinimapWithTarget(_activeCharacter, target.X, target.Y, target.Z));
        }

        private void BuildCityFallbackCoordinates()
        {
            _cityFallbackCoordinates.Clear();

            var groupedByCity = _allPlaceViewModels
                .Where(vm => vm.PrimaryCoordinate != null && !string.IsNullOrWhiteSpace(vm.City))
                .GroupBy(vm => NormalizeCityKey(vm.City))
                .Where(group => !string.IsNullOrWhiteSpace(group.Key));

            foreach(var group in groupedByCity)
            {
                var coordinates = group
                    .Select(vm => vm.PrimaryCoordinate!)
                    .ToList();

                if(coordinates.Count == 0)
                {
                    continue;
                }

                int avgX = (int)Math.Round(coordinates.Average(c => c.X), MidpointRounding.AwayFromZero);
                int avgY = (int)Math.Round(coordinates.Average(c => c.Y), MidpointRounding.AwayFromZero);
                byte avgZ = (byte)Math.Round(coordinates.Average(c => c.Z), MidpointRounding.AwayFromZero);

                _cityFallbackCoordinates[group.Key] = new HuntingPlaceCoordinate(avgX, avgY, avgZ);
            }
        }

        private static string NormalizeCityKey(string? city)
        {
            if(string.IsNullOrWhiteSpace(city))
            {
                return string.Empty;
            }

            return city.Trim()
                       .Replace('’', '\'')
                       .ToLowerInvariant();
        }

        private static bool IsDevModeEnabled()
        {
#if DEBUG
            return true;
#else
            string? env = Environment.GetEnvironmentVariable("THM_DEV_MODE");
            return string.Equals(env, "1", StringComparison.Ordinal);
#endif
        }

        private static int? GetLevelForVocation(HuntingPlaceEntitys place, string vocation)
        {
            return vocation.ToLower() switch
            {
                "sorcerer" or "druid" or "sorcerer/druid" => place.LevelMages,
                "knight" or "elite knight" => place.LevelKnights,
                "paladin" or "royal paladin" => place.LevelPaladins,
                _ => place.LevelMages ?? place.LevelKnights ?? place.LevelPaladins
            };
        }

        private sealed class LocalStatsAccumulator
        {
            public int SessionCount { get; set; }
            public double SumXpPerHour { get; set; }
            public double SumLootPerHour { get; set; }
            public double SumBalancePerHour { get; set; }

            public HuntingPlaceLocalStats ToStats()
            {
                return new HuntingPlaceLocalStats(
                    SessionCount,
                    SessionCount > 0 ? SumXpPerHour / SessionCount : 0d,
                    SessionCount > 0 ? SumLootPerHour / SessionCount : 0d,
                    SessionCount > 0 ? SumBalancePerHour / SessionCount : 0d,
                    SumXpPerHour,
                    SumLootPerHour,
                    SumBalancePerHour);
            }
        }
    }

    public sealed record HuntingPlaceLocalStats(
        int SessionCount,
        double AvgXpPerHour,
        double AvgLootPerHour,
        double AvgBalancePerHour,
        double SumXpPerHour,
        double SumLootPerHour,
        double SumBalancePerHour);

    public sealed class HuntingPlaceItemViewModel : ObservableObject
    {
        private readonly ILocalizationService? _localizationService;
        private readonly HuntingPlaceLocationParseResult _parsedLocation;
        private readonly bool _isDevMode;
        private HuntingPlaceLocalStats? _localStats;

        public HuntingPlaceItemViewModel(
            HuntingPlaceEntitys place,
            ILocalizationService? localizationService = null,
            bool isDevMode = false,
            HuntingPlaceLocalStats? localStats = null)
        {
            Place = place;
            _localizationService = localizationService;
            _isDevMode = isDevMode;
            _parsedLocation = HuntingPlaceLocationParser.Parse(place.Location);
            _localStats = localStats;
        }

        public HuntingPlaceEntitys Place { get; }

        public string Name => Place.Name;

        public string Location => string.IsNullOrWhiteSpace(_parsedLocation.CleanedLocation)
            ? (_localizationService?["HuntingPlaces_LocationUnknown"] ?? "Unknown")
            : _parsedLocation.CleanedLocation;

        public string City => Place.City;

        public IReadOnlyList<HuntingPlaceCoordinate> Coordinates => _parsedLocation.Coordinates;

        public HuntingPlaceCoordinate? PrimaryCoordinate => Coordinates.Count > 0 ? Coordinates[0] : null;

        public string CoordinateDisplay
        {
            get
            {
                if (Coordinates.Count == 0)
                {
                    return _localizationService?["HuntingPlaces_MapNoCoordinates"] ?? "No coordinates";
                }

                if (Coordinates.Count == 1)
                {
                    return Coordinates[0].Display;
                }

                return $"{Coordinates[0].Display} (+{Coordinates.Count - 1})";
            }
        }

        public string? DebugMapTooltip
        {
            get
            {
                if(!_isDevMode)
                {
                    return null;
                }

                if(PrimaryCoordinate == null)
                {
                    return "No exact parsed coordinates in location text.";
                }

                return $"Internal: {PrimaryCoordinate.X},{PrimaryCoordinate.Y},{PrimaryCoordinate.Z} | External: {PrimaryCoordinate.Display}";
            }
        }

        public string LevelDisplay
        {
            get
            {
                List<string> levels = new();
                if(_localizationService != null)
                {
                    if(Place.LevelMages.HasValue)
                    {
                        levels.Add(string.Format(_localizationService["HuntingPlaces_LevelMages"], Place.LevelMages));
                    }
                    if(Place.LevelKnights.HasValue)
                    {
                        levels.Add(string.Format(_localizationService["HuntingPlaces_LevelKnights"], Place.LevelKnights));
                    }
                    if(Place.LevelPaladins.HasValue)
                    {
                        levels.Add(string.Format(_localizationService["HuntingPlaces_LevelPaladins"], Place.LevelPaladins));
                    }
                    return levels.Count > 0 ? string.Join(" | ", levels) : _localizationService["HuntingPlaces_NotAvailable"];
                }
                else
                {
                    if(Place.LevelMages.HasValue)
                    {
                        levels.Add($"Mages: {Place.LevelMages}");
                    }
                    if(Place.LevelKnights.HasValue)
                    {
                        levels.Add($"Knights: {Place.LevelKnights}");
                    }
                    if(Place.LevelPaladins.HasValue)
                    {
                        levels.Add($"Paladins: {Place.LevelPaladins}");
                    }
                    return levels.Count > 0 ? string.Join(" | ", levels) : "N/A";
                }
            }
        }

        public string ExpDisplay
        {
            get
            {
                if(Place.Experience.HasValue)
                {
                    return $"{Place.Experience:N0} {Place.ExperienceStar ?? ""}";
                }
                return _localizationService?["HuntingPlaces_NotAvailable"] ?? "N/A";
            }
        }

        public string LootDisplay
        {
            get
            {
                if(Place.LootValue.HasValue)
                {
                    return $"{Place.LootValue:N0}k {Place.LootStar ?? ""}";
                }
                return _localizationService?["HuntingPlaces_NotAvailable"] ?? "N/A";
            }
        }

        public bool HasLocalStats => _localStats is { SessionCount: > 0 };

        public int LocalSessionCount => _localStats?.SessionCount ?? 0;

        public string LocalXpPerHourDisplay => HasLocalStats
            ? $"{_localStats!.AvgXpPerHour:N0}"
            : "-";

        public string LocalLootPerHourDisplay => HasLocalStats
            ? $"{_localStats!.AvgLootPerHour:N0}"
            : "-";

        public string LocalBalancePerHourDisplay => HasLocalStats
            ? $"{_localStats!.AvgBalancePerHour:N0}"
            : "-";

        public void SetLocalStats(HuntingPlaceLocalStats? localStats)
        {
            _localStats = localStats;
            OnPropertyChanged(nameof(HasLocalStats));
            OnPropertyChanged(nameof(LocalSessionCount));
            OnPropertyChanged(nameof(LocalXpPerHourDisplay));
            OnPropertyChanged(nameof(LocalLootPerHourDisplay));
            OnPropertyChanged(nameof(LocalBalancePerHourDisplay));
        }

        // Refresh localized strings when language changes
        public void RefreshLocalizedStrings()
        {
            OnPropertyChanged(nameof(Location));
            OnPropertyChanged(nameof(CoordinateDisplay));
            OnPropertyChanged(nameof(LevelDisplay));
            OnPropertyChanged(nameof(ExpDisplay));
            OnPropertyChanged(nameof(LootDisplay));
        }
    }

    // INavigationAware implementation for HuntingPlacesViewModel (using partial class)
    public sealed partial class HuntingPlacesViewModel
    {
        public void OnNavigatedTo(object? parameter)
        {
            if(parameter is HuntingPlacesWithCharacter characterParam)
            {
                _activeCharacter = characterParam.Character;
                HasCharacterContext = true;
                _ = LoadHuntingPlacesAsync(characterParam.Character.Vocation);
                return;
            }

            if(parameter is HuntingPlacesWithVocation vocationParam)
            {
                _activeCharacter = null;
                HasCharacterContext = false;
                OnlyHuntedPlaces = false;
                _ = LoadHuntingPlacesAsync(vocationParam.Vocation);
            }
        }

        public void OnNavigatedFrom()
        {
            // No cleanup needed
        }
    }
}
