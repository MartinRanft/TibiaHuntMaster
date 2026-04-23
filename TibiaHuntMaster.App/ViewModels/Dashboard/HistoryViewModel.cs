using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.App.Services.Database;
using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.App.ViewModels.Analyzer;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Character;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.Hunts;

using static TibiaHuntMaster.App.Services.Navigation.NavigationParameters;

namespace TibiaHuntMaster.App.ViewModels.Dashboard
{
    public sealed partial class HistoryViewModel : ViewModelBase, INavigationAware
    {
        private const int PageSize = 10;

        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly HuntMergerService _mergerService;
        private readonly HuntGroupingService _groupingService;
        private readonly INavigationService _navigationService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<HistoryViewModel> _logger;
        private readonly List<HistoryItemViewModel> _allItems = [];
        private bool _suppressDateFilterRefresh;
        private bool _suppressGoalFilterRefresh;

        public HistoryViewModel(
            IDbContextFactory<AppDbContext> dbFactory,
            HuntMergerService mergerService,
            HuntGroupingService groupingService,
            INavigationService navigationService,
            ILocalizationService localizationService,
            ILogger<HistoryViewModel> logger)
        {
            _dbFactory = dbFactory;
            _mergerService = mergerService;
            _groupingService = groupingService;
            _navigationService = navigationService;
            _localizationService = localizationService;
            _logger = logger;
            _statusMessage = _localizationService["History_StatusEmpty"];
            UpdateTypeFilterOptions();

            // Subscribe to language changes
            _localizationService.PropertyChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Refresh UI strings when language changes
            UpdateTypeFilterOptions();
            UpdateStatusMessage();

            // Refresh goal info if a filter is active
            if (IsGoalFilterActive)
            {
                _ = RefreshList();
            }

            // Refresh each item's localized properties
            foreach (var item in Items)
            {
                item.RefreshLocalizedStrings();
            }

            foreach (var item in _allItems.Except(Items))
            {
                item.RefreshLocalizedStrings();
            }

            OnPropertyChanged(nameof(DateRangeFilterDisplay));
            OnPropertyChanged(nameof(DraftDateRangeFilterDisplay));
        }

        private void UpdateStatusMessage()
        {
            if (TotalFilteredItems == 0)
            {
                StatusMessage = _hasHiddenHistoryDueToFilter
                    ? _localizationService["History_StatusFilteredEmpty"]
                    : _localizationService["History_StatusEmpty"];
            }
            else
            {
                StatusMessage = string.Format(_localizationService["History_EntriesLoaded"], TotalFilteredItems);
            }

            UpdatePaginationSummary();
        }
        [ObservableProperty]private ObservableCollection<CharacterGoalEntity> _availableGoals = [];

        private string _currentCharName = string.Empty;

        [ObservableProperty]private string _goalDateInfo = string.Empty;
        [ObservableProperty]private string _goalStatusInfo = string.Empty;
        [ObservableProperty]private bool _hasSelection;
        [ObservableProperty]private bool _isDemergeVisible;
        [ObservableProperty]private bool _isLoading;
        [ObservableProperty]private bool _isMergeVisible;
        [ObservableProperty]private ObservableCollection<HistoryItemViewModel> _items = [];
        [ObservableProperty]private DateTimeOffset? _fromDateFilter;
        [ObservableProperty]private DateTimeOffset? _toDateFilter;
        [ObservableProperty]private DateTimeOffset? _draftFromDateFilter;
        [ObservableProperty]private DateTimeOffset? _draftToDateFilter;
        [ObservableProperty]private bool _isDateRangeDialogOpen;
        [ObservableProperty]private int _currentPage = 1;
        [ObservableProperty]private int _totalPages = 1;
        [ObservableProperty]private int _totalFilteredItems;
        [ObservableProperty]private string _paginationSummary = string.Empty;
        [ObservableProperty]private ObservableCollection<string> _typeFilterOptions = [];
        private bool _hasHiddenHistoryDueToFilter;
        private CancellationTokenSource? _loadCts;
        [ObservableProperty]private CharacterGoalEntity? _selectedGoalFilter;

        // --- FILTER ---
        [ObservableProperty]private int _selectedTypeFilterIndex;
        [ObservableProperty]private string _statusMessage;

        /// <summary>
        /// Parameterless constructor for XAML designer support only.
        /// Do not use in production code - use the DI constructor instead.
        /// </summary>
        [Obsolete("Designer only - use DI constructor")]
        public HistoryViewModel()
        {
            _dbFactory = null!;
            _mergerService = null!;
            _groupingService = null!;
            _navigationService = null!;
            _localizationService = null!;
            _logger = null!;
            _statusMessage = "No hunts found.";
            _typeFilterOptions =
            [
                "All Types",
                "Solo",
                "Party"
            ];
        }

        // --- NEU: GOAL HEADER INFO ---
        // Steuert die Sichtbarkeit des Info-Kastens
        public bool IsGoalFilterActive => SelectedGoalFilter != null && SelectedGoalFilter.Id > 0;

        public bool HasPagination => TotalFilteredItems > 0;

        public bool HasDateRangeFilter => FromDateFilter.HasValue || ToDateFilter.HasValue;

        public string DateRangeFilterDisplay
        {
            get
            {
                return FormatDateRangeDisplay(FromDateFilter, ToDateFilter);
            }
        }

        public string DraftDateRangeFilterDisplay
        {
            get
            {
                return FormatDateRangeDisplay(DraftFromDateFilter, DraftToDateFilter);
            }
        }

        public bool CanGoToPreviousPage => CurrentPage > 1;

        public bool CanGoToNextPage => CurrentPage < TotalPages;

        // INavigationAware Implementation
        public void OnNavigatedTo(object? parameter)
        {
            if(parameter is HistoryWithCharacter historyParam)
            {
                _ = LoadHistoryAsync(historyParam.CharacterName);
            }
            else if(parameter is HistoryWithGoalFilter goalParam)
            {
                // Load history and then set goal filter
                _ = LoadHistoryAsync(goalParam.CharacterName).ContinueWith(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        CharacterGoalEntity? match = AvailableGoals.FirstOrDefault(g => g.Id == goalParam.Goal.Id);
                        if(match != null)
                        {
                            SelectedGoalFilter = match;
                        }
                    });
                });
            }
        }

        public void OnNavigatedFrom()
        {
            // Cancel any pending load operations
            _loadCts?.Cancel();
        }

        private void UpdateTypeFilterOptions()
        {
            TypeFilterOptions.Clear();
            TypeFilterOptions.Add(_localizationService["History_AllTypes"]);
            TypeFilterOptions.Add(_localizationService["History_Solo"]);
            TypeFilterOptions.Add(_localizationService["History_Party"]);

            if(SelectedTypeFilterIndex < 0 || SelectedTypeFilterIndex >= TypeFilterOptions.Count)
            {
                SelectedTypeFilterIndex = 0;
            }
        }

        partial void OnSelectedTypeFilterIndexChanged(int value)
        {
            ResetToFirstPageAndRefresh();
        }

        partial void OnSelectedGoalFilterChanged(CharacterGoalEntity? value)
        {
            OnPropertyChanged(nameof(IsGoalFilterActive));

            if(_suppressGoalFilterRefresh)
            {
                return;
            }

            ResetToFirstPageAndRefresh();
        }

        partial void OnFromDateFilterChanged(DateTimeOffset? value)
        {
            OnPropertyChanged(nameof(HasDateRangeFilter));
            OnPropertyChanged(nameof(DateRangeFilterDisplay));

            if(_suppressDateFilterRefresh)
            {
                return;
            }

            if(value.HasValue && ToDateFilter.HasValue && value.Value.Date > ToDateFilter.Value.Date)
            {
                _suppressDateFilterRefresh = true;
                ToDateFilter = value;
                _suppressDateFilterRefresh = false;
            }

            ResetToFirstPageAndRefresh();
        }

        partial void OnToDateFilterChanged(DateTimeOffset? value)
        {
            OnPropertyChanged(nameof(HasDateRangeFilter));
            OnPropertyChanged(nameof(DateRangeFilterDisplay));

            if(_suppressDateFilterRefresh)
            {
                return;
            }

            if(value.HasValue && FromDateFilter.HasValue && value.Value.Date < FromDateFilter.Value.Date)
            {
                _suppressDateFilterRefresh = true;
                FromDateFilter = value;
                _suppressDateFilterRefresh = false;
            }

            ResetToFirstPageAndRefresh();
        }

        partial void OnDraftFromDateFilterChanged(DateTimeOffset? value)
        {
            OnPropertyChanged(nameof(DraftDateRangeFilterDisplay));
        }

        partial void OnDraftToDateFilterChanged(DateTimeOffset? value)
        {
            OnPropertyChanged(nameof(DraftDateRangeFilterDisplay));
        }

        partial void OnCurrentPageChanged(int value)
        {
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
            UpdatePaginationSummary();
            ApplyPagination();
        }

        partial void OnTotalPagesChanged(int value)
        {
            OnPropertyChanged(nameof(CanGoToNextPage));
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
            UpdatePaginationSummary();
        }

        partial void OnTotalFilteredItemsChanged(int value)
        {
            OnPropertyChanged(nameof(HasPagination));
            UpdatePaginationSummary();
        }

        private void ResetToFirstPageAndRefresh()
        {
            CurrentPage = 1;
            _ = RefreshList();
        }

        private async Task RefreshList()
        {
            if(!string.IsNullOrEmpty(_currentCharName))
            {
                await LoadHistoryAsync(_currentCharName);
            }
        }

        public async Task LoadHistoryAsync(string charName)
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            CancellationToken token = _loadCts.Token;

            IsLoading = true;
            _currentCharName = charName;

            try
            {
                await Task.Delay(50, token);
                await using AppDbContext db = await _dbFactory.CreateDbContextAsync(token);
                SqliteSchemaRepair.EnsureCriticalSchema(db);

                string requestedName = charName.Trim();
                CharacterEntity? character = await db.Characters
                                                    .AsNoTracking()
                                                    .FirstOrDefaultAsync(x => x.Name == requestedName, token);

                if(character == null)
                {
                    // Fallback to in-memory case-insensitive match to avoid DB collation differences across platforms.
                    List<CharacterEntity> allCharacters = await db.Characters.AsNoTracking().ToListAsync(token);
                    character = allCharacters.FirstOrDefault(c =>
                        string.Equals(c.Name?.Trim(), requestedName, StringComparison.OrdinalIgnoreCase));
                }

                if(character == null)
                {
                    ClearLoadedItems();
                    _hasHiddenHistoryDueToFilter = false;
                    StatusMessage = $"Character '{requestedName}' not found for history.";
                    IsLoading = false;
                    _logger.LogWarning("Character '{RequestedName}' not found in database for history loading", requestedName);
                    return;
                }

                int characterId = character.Id;
                _logger.LogDebug("Loading history for character '{CharacterName}' (ID: {CharacterId})", character.Name, characterId);

                // 1. Goals laden (Dropdown)
                if(AvailableGoals.Count == 0)
                {
                    AvailableGoals.Add(new CharacterGoalEntity
                    {
                        Title = _localizationService["History_AllHuntsNoGoal"],
                        Id = -1
                    });

                    if(character != null)
                    {
                        List<CharacterGoalEntity> goals = await db.CharacterGoals.AsNoTracking().Where(g => g.CharacterId == characterId).OrderByDescending(g => g.IsActive).ToListAsync(token);
                        foreach(CharacterGoalEntity g in goals)
                        {
                            AvailableGoals.Add(g);
                        }
                    }

                    _suppressGoalFilterRefresh = true;
                    try
                    {
                        SelectedGoalFilter = AvailableGoals.First();
                    }
                    finally
                    {
                        _suppressGoalFilterRefresh = false;
                    }
                }

                // 2. Filter Logik vorbereiten
                DateTimeOffset filterStartDate = DateTimeOffset.MinValue;
                DateTimeOffset filterEndDate = DateTimeOffset.MaxValue;
                bool filterByGoal = SelectedGoalFilter != null && SelectedGoalFilter.Id > 0;
                DateTimeOffset? dateRangeStart = FromDateFilter.HasValue
                    ? GetStartOfDay(FromDateFilter.Value)
                    : null;
                DateTimeOffset? dateRangeEndExclusive = ToDateFilter.HasValue
                    ? GetStartOfDay(ToDateFilter.Value).AddDays(1)
                    : null;

                // Connections laden wir trotzdem für die Medaillen-Anzeige
                List<HuntGoalConnectionEntity> connections;
                try
                {
                    connections = await db.HuntGoalConnections
                                          .Include(c => c.Goal)
                                          .Where(c => c.Goal.CharacterId == characterId)
                                          .ToListAsync(token);
                }
                catch (SqliteException ex) when (IsMissingHuntGoalConnectionsTable(ex))
                {
                    // Keep history usable on older/broken schemas; only medal metadata is unavailable.
                    connections = [];
                }

                if(filterByGoal)
                {
                    // A) Startdatum setzen (Hunts davor zählen nicht)
                    filterStartDate = SelectedGoalFilter!.CreatedAt;

                    // B) Info Text bauen
                    string startStr = filterStartDate.ToLocalTime().ToString("dd.MM.yyyy");
                    GoalDateInfo = string.Format(_localizationService["History_TrackingStarted"], startStr);
                    GoalStatusInfo = SelectedGoalFilter.IsCompleted ? _localizationService["History_StatusCompleted"] : _localizationService["History_StatusInProgress"];

                    // C) Wenn fertig: Enddatum finden (Wann war der Finisher-Hunt?)
                    if(SelectedGoalFilter.IsCompleted)
                    {
                        HuntGoalConnectionEntity? finisher = connections.FirstOrDefault(c => c.CharacterGoalId == SelectedGoalFilter.Id && c.IsFinisher);
                        if(finisher != null)
                        {
                            // Datum des Finisher Hunts holen
                            DateTimeOffset finishDate = DateTimeOffset.UtcNow; // Fallback

                            if(finisher.HuntSessionId.HasValue)
                            {
                                HuntSessionEntity? s = await db.HuntSessions.FindAsync(finisher.HuntSessionId.Value);
                                if(s != null)
                                {
                                    finishDate = s.SessionStartTime.Year > 2000 ? s.SessionStartTime : s.ImportedAt;
                                }
                            }
                            else if(finisher.TeamHuntSessionId.HasValue)
                            {
                                TeamHuntSessionEntity? t = await db.TeamHuntSessions.FindAsync(finisher.TeamHuntSessionId.Value);
                                if(t != null)
                                {
                                    finishDate = t.SessionStartTime.Year > 2000 ? t.SessionStartTime : t.ImportedAt;
                                }
                            }

                            filterEndDate = finishDate;
                            GoalDateInfo += string.Format(_localizationService["History_Finished"], finishDate.ToLocalTime().ToString("dd.MM.yyyy"));
                        }
                    }
                }

                // 3. SOLO LADEN (Mit Datums-Filter)
                IQueryable<HuntSessionEntity> soloQuery = db.HuntSessions
                                                            .AsNoTracking()
                                                            .Include(s => s.SupplyAdjustments)
                                                            .Where(s => s.CharacterId == characterId && s.HuntGroupId == null);

                if(SelectedTypeFilterIndex == 2)
                {
                    soloQuery = soloQuery.Where(x => false);
                }

                // FILTER FIX: Wir schauen auf das Datum, nicht auf die Connection-ID!
                if(filterByGoal)
                {
                    // Wir erlauben ImportAt ODER StartTime >= CreatedAt
                    soloQuery = soloQuery.Where(s => s.SessionStartTime >= filterStartDate || s.ImportedAt >= filterStartDate);
                    // Optional: Cap beim Enddatum (falls fertig)
                    if(SelectedGoalFilter!.IsCompleted)
                    {
                        soloQuery = soloQuery.Where(s => s.SessionStartTime <= filterEndDate);
                    }
                }

                if(dateRangeStart.HasValue)
                {
                    DateTimeOffset start = dateRangeStart.Value;
                    soloQuery = soloQuery.Where(s => s.SessionStartTime >= start || s.ImportedAt >= start);
                }

                if(dateRangeEndExclusive.HasValue)
                {
                    DateTimeOffset endExclusive = dateRangeEndExclusive.Value;
                    soloQuery = soloQuery.Where(s => s.SessionStartTime < endExclusive || s.ImportedAt < endExclusive);
                }

                List<HuntSessionEntity> singleSessions = await soloQuery.ToListAsync(token);

                // 4. TEAM LADEN (Mit Datums-Filter)
                IQueryable<TeamHuntSessionEntity> teamQuery = db.TeamHuntSessions
                                                                .AsNoTracking()
                                                                .Include(t => t.Members)
                                                                .Where(s => s.CharacterId == characterId);

                if(SelectedTypeFilterIndex == 1)
                {
                    teamQuery = teamQuery.Where(x => false);
                }

                if(filterByGoal)
                {
                    teamQuery = teamQuery.Where(t => t.SessionStartTime >= filterStartDate || t.ImportedAt >= filterStartDate);
                    if(SelectedGoalFilter!.IsCompleted)
                    {
                        teamQuery = teamQuery.Where(t => t.SessionStartTime <= filterEndDate);
                    }
                }

                if(dateRangeStart.HasValue)
                {
                    DateTimeOffset start = dateRangeStart.Value;
                    teamQuery = teamQuery.Where(t => t.SessionStartTime >= start || t.ImportedAt >= start);
                }

                if(dateRangeEndExclusive.HasValue)
                {
                    DateTimeOffset endExclusive = dateRangeEndExclusive.Value;
                    teamQuery = teamQuery.Where(t => t.SessionStartTime < endExclusive || t.ImportedAt < endExclusive);
                }

                List<TeamHuntSessionEntity> teamSessions = await teamQuery.ToListAsync(token);

                // 5. GROUPS LADEN
                List<HuntGroupEntity> groups = await db.HuntGroups
                                                       .AsNoTracking()
                                                       .Include(g => g.Sessions).ThenInclude(s => s.SupplyAdjustments)
                                                       .Where(g => g.Sessions.Any(s => s.CharacterId == characterId))
                                                       .ToListAsync(token);

                if(SelectedTypeFilterIndex == 2)
                {
                    groups.Clear();
                }

                // Gruppen filtern: Zeigen, wenn min. 1 Session im Zeitraum liegt
                if(filterByGoal)
                {
                    // Wir filtern im Speicher, da DateTime-Vergleiche in Unter-Listen in EF Core SQLite tricky sein können
                    groups = groups.Where(g => g.Sessions.Any(s =>
                    (s.SessionStartTime >= filterStartDate || s.ImportedAt >= filterStartDate) &&
                    (!SelectedGoalFilter!.IsCompleted || s.SessionStartTime <= filterEndDate)
                    )).ToList();
                }

                if(dateRangeStart.HasValue || dateRangeEndExclusive.HasValue)
                {
                    groups = groups.Where(g => g.Sessions.Any(s => SessionMatchesDateRange(s.SessionStartTime, s.ImportedAt, dateRangeStart, dateRangeEndExclusive))).ToList();
                }

                if(token.IsCancellationRequested)
                {
                    return;
                }

                // 6. ZUSAMMENBAUEN
                List<HistoryItemViewModel> allVms = new();

                foreach(HuntSessionEntity s in singleSessions)
                {
                    HistoryItemViewModel vm = new(s, _localizationService);
                    CheckMedal(vm, connections, s.Id, false);
                    allVms.Add(vm);
                }
                foreach(TeamHuntSessionEntity t in teamSessions)
                {
                    HistoryItemViewModel vm = new(t, _localizationService);
                    CheckMedal(vm, connections, t.Id, true);
                    allVms.Add(vm);
                }
                foreach(HuntGroupEntity g in groups)
                {
                    HistoryItemViewModel vm = new(g, g.Sessions, _localizationService);
                    foreach(HistoryItemViewModel child in vm.Children)
                    {
                        if(child.Session != null)
                        {
                            CheckMedal(child, connections, child.Session.Id, false);
                        }
                    }
                    allVms.Add(vm);
                }

                ClearLoadedItems();

                foreach(HistoryItemViewModel item in allVms.OrderByDescending(GetSortDate))
                {
                    AttachHistoryItem(item);
                    _allItems.Add(item);
                }

                TotalFilteredItems = _allItems.Count;
                UpdateTotalPages();
                ApplyPagination();

                _hasHiddenHistoryDueToFilter = TotalFilteredItems == 0 &&
                                               await HasAnyHistoryDataAsync(db, characterId, token);
                UpdateStatusMessage();
                UpdateSelectionState();

                _logger.LogInformation("Successfully loaded {ItemCount} history items for character '{CharacterName}' (ID: {CharacterId}). Solo: {SoloCount}, Team: {TeamCount}, Groups: {GroupCount}",
                    Items.Count, _currentCharName, characterId, singleSessions.Count, teamSessions.Count, groups.Count);
            }
            catch (OperationCanceledException)
            {
                _hasHiddenHistoryDueToFilter = false;
                _logger.LogDebug("History loading cancelled for character '{CharacterName}'", _currentCharName);
            }
            catch (Exception ex)
            {
                _hasHiddenHistoryDueToFilter = false;
                StatusMessage = $"Failed to load history: {ex.GetType().Name}: {ex.Message}";
                _logger.LogError(ex, "Failed to load history for character '{CharacterName}'", _currentCharName);
            }
            finally
            {
                if(!token.IsCancellationRequested)
                {
                    IsLoading = false;
                }
            }
        }

        private static bool SessionMatchesDateRange(
            DateTimeOffset sessionStartTime,
            DateTimeOffset importedAt,
            DateTimeOffset? startInclusive,
            DateTimeOffset? endExclusive)
        {
            DateTimeOffset effectiveDate = sessionStartTime.Year > 2000 ? sessionStartTime : importedAt;

            if(startInclusive.HasValue && effectiveDate < startInclusive.Value)
            {
                return false;
            }

            if(endExclusive.HasValue && effectiveDate >= endExclusive.Value)
            {
                return false;
            }

            return true;
        }

        private static DateTimeOffset GetStartOfDay(DateTimeOffset value)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, value.Offset);
        }

        private string FormatDateRangeDisplay(DateTimeOffset? fromDate, DateTimeOffset? toDate)
        {
            if(!fromDate.HasValue && !toDate.HasValue)
            {
                return _localizationService["History_AllDates"];
            }

            string start = fromDate.HasValue
                ? fromDate.Value.ToLocalTime().ToString("dd.MM.yyyy")
                : "…";
            string end = toDate.HasValue
                ? toDate.Value.ToLocalTime().ToString("dd.MM.yyyy")
                : "…";

            return $"{start} - {end}";
        }

        private void CheckMedal(HistoryItemViewModel vm, List<HuntGoalConnectionEntity> connections, int id, bool isTeam)
        {
            HuntGoalConnectionEntity? match = connections.FirstOrDefault(c =>
            c.IsFinisher &&
            ((!isTeam && c.HuntSessionId == id) || (isTeam && c.TeamHuntSessionId == id)));

            if(match != null)
            {
                vm.IsFinisher = true;
                vm.GoalName = match.Goal.Title;
            }
        }

        private static bool IsMissingHuntGoalConnectionsTable(SqliteException ex)
        {
            if (ex.SqliteErrorCode != 1)
            {
                return false;
            }

            return ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase) &&
                   ex.Message.Contains("HuntGoalConnections", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<bool> HasAnyHistoryDataAsync(AppDbContext db, int characterId, CancellationToken token)
        {
            if (await db.HuntSessions.AsNoTracking().AnyAsync(s => s.CharacterId == characterId, token))
            {
                return true;
            }

            return await db.TeamHuntSessions.AsNoTracking().AnyAsync(s => s.CharacterId == characterId, token);
        }

        // ... (Restliche Methoden GetSortDate, AddHistoryItem etc. bleiben exakt gleich) ...

        private DateTimeOffset GetSortDate(HistoryItemViewModel vm)
        {
            if(vm.IsGroup)
            {
                DateTimeOffset maxDate = vm.Children.Select(c => c.Session?.SessionStartTime.Year > 2000 ? c.Session!.SessionStartTime : c.Session?.ImportedAt ?? DateTimeOffset.MinValue)
                                           .DefaultIfEmpty(DateTimeOffset.MinValue).Max();
                return maxDate > DateTimeOffset.MinValue ? maxDate : vm.Group?.CreatedAt ?? DateTimeOffset.MinValue;
            }
            if(vm.Session != null)
            {
                return vm.Session.SessionStartTime.Year > 2000 ? vm.Session.SessionStartTime : vm.Session.ImportedAt;
            }
            if(vm.TeamSession != null)
            {
                return vm.TeamSession.SessionStartTime.Year > 2000 ? vm.TeamSession.SessionStartTime : vm.TeamSession.ImportedAt;
            }
            return DateTimeOffset.MinValue;
        }

        private void AttachHistoryItem(HistoryItemViewModel vm)
        {
            vm.PropertyChanged += (sender, args) =>
            {
                if(args.PropertyName == nameof(HistoryItemViewModel.IsSelected))
                {
                    UpdateSelectionState();
                }
            };
            foreach(HistoryItemViewModel child in vm.Children)
            {
                child.PropertyChanged += (sender, args) =>
                {
                    if(args.PropertyName == nameof(HistoryItemViewModel.IsSelected))
                    {
                        UpdateSelectionState();
                    }
                };
            }
        }

        private void ClearLoadedItems()
        {
            ClearAllSelections();
            _allItems.Clear();
            Items.Clear();
            TotalFilteredItems = 0;
            TotalPages = 1;
            CurrentPage = 1;
            UpdatePaginationSummary();
        }

        private void UpdateTotalPages()
        {
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalFilteredItems / (double)PageSize));
            if(CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }

        private void ApplyPagination()
        {
            Items.Clear();

            if(_allItems.Count == 0)
            {
                UpdateSelectionState();
                OnPropertyChanged(nameof(HasPagination));
                return;
            }

            int safePage = Math.Clamp(CurrentPage, 1, TotalPages);
            if(safePage != CurrentPage)
            {
                CurrentPage = safePage;
                return;
            }

            foreach(HistoryItemViewModel item in _allItems.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            {
                Items.Add(item);
            }

            UpdateSelectionState();
            OnPropertyChanged(nameof(HasPagination));
        }

        private void UpdatePaginationSummary()
        {
            if(TotalFilteredItems == 0)
            {
                PaginationSummary = string.Empty;
                OnPropertyChanged(nameof(HasPagination));
                return;
            }

            int firstItemIndex = ((CurrentPage - 1) * PageSize) + 1;
            int lastItemIndex = Math.Min(CurrentPage * PageSize, TotalFilteredItems);
            PaginationSummary = string.Format(
                _localizationService["History_PaginationSummary"],
                firstItemIndex,
                lastItemIndex,
                TotalFilteredItems,
                CurrentPage,
                TotalPages);
            OnPropertyChanged(nameof(HasPagination));
        }

        private void ClearAllSelections()
        {
            foreach(HistoryItemViewModel item in _allItems)
            {
                item.IsSelected = false;
                foreach(HistoryItemViewModel child in item.Children)
                {
                    child.IsSelected = false;
                }
            }
        }

        private void UpdateSelectionState()
        {
            bool anySelected = false;
            int standaloneSelectedCount = 0;
            bool groupChildSelected = false;
            foreach(HistoryItemViewModel item in Items)
            {
                if(item.IsSelected)
                {
                    anySelected = true;
                    if(!item.IsGroup && item.Session != null)
                    {
                        standaloneSelectedCount++;
                    }
                }
                foreach(HistoryItemViewModel child in item.Children)
                {
                    if(child.IsSelected)
                    {
                        anySelected = true;
                        groupChildSelected = true;
                    }
                }
            }
            HasSelection = anySelected;
            IsDemergeVisible = groupChildSelected;
            IsMergeVisible = !IsDemergeVisible && standaloneSelectedCount > 1;
        }

        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        private void PreviousPage()
        {
            if(!CanGoToPreviousPage)
            {
                return;
            }

            ClearAllSelections();
            CurrentPage--;
        }

        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        private void NextPage()
        {
            if(!CanGoToNextPage)
            {
                return;
            }

            ClearAllSelections();
            CurrentPage++;
        }

        [RelayCommand]
        private void ClearDateFilters()
        {
            _suppressDateFilterRefresh = true;
            FromDateFilter = null;
            ToDateFilter = null;
            _suppressDateFilterRefresh = false;
            ResetToFirstPageAndRefresh();
        }

        [RelayCommand]
        private void OpenDateRangeDialog()
        {
            DraftFromDateFilter = FromDateFilter;
            DraftToDateFilter = ToDateFilter;
            IsDateRangeDialogOpen = true;
        }

        [RelayCommand]
        private void CancelDateRangeDialog()
        {
            DraftFromDateFilter = FromDateFilter;
            DraftToDateFilter = ToDateFilter;
            IsDateRangeDialogOpen = false;
        }

        [RelayCommand]
        private void ConfirmDateRangeDialog()
        {
            ApplyDraftDateRange();
        }

        [RelayCommand]
        private void DismissDateRangeDialog()
        {
            ApplyDraftDateRange();
        }

        private void ApplyDraftDateRange()
        {
            bool changed =
                !Nullable.Equals(FromDateFilter, DraftFromDateFilter) ||
                !Nullable.Equals(ToDateFilter, DraftToDateFilter);

            _suppressDateFilterRefresh = true;
            FromDateFilter = DraftFromDateFilter;
            ToDateFilter = DraftToDateFilter;
            _suppressDateFilterRefresh = false;
            IsDateRangeDialogOpen = false;
            OnPropertyChanged(nameof(HasDateRangeFilter));
            OnPropertyChanged(nameof(DateRangeFilterDisplay));

            if(changed)
            {
                ResetToFirstPageAndRefresh();
            }
        }

        [RelayCommand]
        private async Task DemergeSelected()
        {
            IsLoading = true;
            try
            {
                List<int> sessionsToDemerge = Items.Where(x => x.IsGroup).SelectMany(g => g.IsSelected ? g.Children : g.Children.Where(c => c.IsSelected)).Select(c => c.Session!.Id).Distinct()
                                                   .ToList();
                if(sessionsToDemerge.Count == 0)
                {
                    return;
                }
                foreach(int sessionId in sessionsToDemerge)
                {
                    await _groupingService.RemoveSessionFromGroupAsync(sessionId);
                }
                if(!string.IsNullOrEmpty(_currentCharName))
                {
                    await LoadHistoryAsync(_currentCharName);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task MergeAndAnalyze()
        {
            HistoryItemViewModel? teamItem = Items.FirstOrDefault(x => x.IsSelected && x.IsTeam);
            if(teamItem != null)
            {
                _navigationService.NavigateTo<HuntAnalyzerViewModel>(
                    new AnalyzerWithTeamSession(_currentCharName, teamItem.TeamSession!)
                );
                return;
            }
            HashSet<int> selectedIds = new();
            foreach(HistoryItemViewModel item in Items)
            {
                if(!item.IsGroup && item.IsSelected && item.Session != null)
                {
                    selectedIds.Add(item.Session.Id);
                }
                if(item.IsGroup)
                {
                    if(item.IsSelected)
                    {
                        foreach(HistoryItemViewModel child in item.Children)
                        {
                            selectedIds.Add(child.Session!.Id);
                        }
                    }
                    else
                    {
                        foreach(HistoryItemViewModel child in item.Children.Where(c => c.IsSelected))
                        {
                            selectedIds.Add(child.Session!.Id);
                        }
                    }
                }
            }
            if(selectedIds.Count == 0)
            {
                return;
            }
            IsLoading = true;
            try
            {
                await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
                List<HuntSessionEntity> fullSessions = await db.HuntSessions.AsNoTracking().Include(s => s.SupplyAdjustments).Include(s => s.LootItems).Include(s => s.KilledMonsters)
                                                               .Where(s => selectedIds.Contains(s.Id)).ToListAsync();
                if(fullSessions.Count == 0)
                {
                    return;
                }
                HuntSessionEntity resultSession = fullSessions.Count == 1 ? fullSessions.First() : _mergerService.MergeSessions(fullSessions);
                _navigationService.NavigateTo<HuntAnalyzerViewModel>(
                    new AnalyzerWithSession(_currentCharName, resultSession, selectedIds.ToList())
                );
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task DeleteSelected()
        {
            List<HistoryItemViewModel> itemsToRemove = Items.Where(x => x.IsSelected).ToList();
            if(itemsToRemove.Count == 0)
            {
                return;
            }
            await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
            foreach(HistoryItemViewModel vm in itemsToRemove)
            {
                if(vm.IsGroup && vm.Group != null)
                {
                    HuntGroupEntity? g = await db.HuntGroups.FindAsync(vm.Group.Id);
                    if(g != null)
                    {
                        db.HuntGroups.Remove(g);
                    }
                }
                else if(vm.Session != null)
                {
                    HuntSessionEntity? entry = await db.HuntSessions.FindAsync(vm.Session.Id);
                    if(entry != null)
                    {
                        db.HuntSessions.Remove(entry);
                    }
                }
                else if(vm.TeamSession != null)
                {
                    TeamHuntSessionEntity? entry = await db.TeamHuntSessions.FindAsync(vm.TeamSession.Id);
                    if(entry != null)
                    {
                        db.TeamHuntSessions.Remove(entry);
                    }
                }
                Items.Remove(vm);
            }
            await db.SaveChangesAsync();
            UpdateSelectionState();
            if(!string.IsNullOrEmpty(_currentCharName))
            {
                await LoadHistoryAsync(_currentCharName);
            }
        }
    }
}
