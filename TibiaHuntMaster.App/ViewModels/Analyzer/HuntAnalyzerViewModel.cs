using System.Collections.ObjectModel;
using System.ComponentModel;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.App.Services;
using TibiaHuntMaster.App.Services.Diagnostics;
using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.App.Services.Summaries;
using TibiaHuntMaster.App.ViewModels.Configuration;
using TibiaHuntMaster.App.ViewModels.Dashboard;
using TibiaHuntMaster.Core.Security;
using TibiaHuntMaster.Core.Hunts;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Services.Analysis;
using TibiaHuntMaster.Infrastructure.Services.Hunts;

using static TibiaHuntMaster.App.Services.Navigation.NavigationParameters;

namespace TibiaHuntMaster.App.ViewModels.Analyzer
{
    public sealed class BankTransferInstruction(string from, string to, long amount)
    {
        public string FromName { get; } = from;

        public string ToName { get; } = to;

        public long Amount { get; } = amount;

        public string CommandText => $"transfer {Amount} to {ToName}";
    }

    public sealed class HuntingPlaceSelectionOption(int? id, string displayName)
    {
        public int? Id { get; } = id;

        public string DisplayName { get; } = displayName;

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public sealed class SummaryFormatOption(HuntSummaryFormat format, string displayName)
    {
        public HuntSummaryFormat Format { get; } = format;

        public string DisplayName { get; } = displayName;

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public sealed class SummaryTemplateOption(HuntSummaryTemplatePreset preset, string displayName)
    {
        public HuntSummaryTemplatePreset Preset { get; } = preset;

        public string DisplayName { get; } = displayName;

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public sealed partial class HuntAnalyzerViewModel : ViewModelBase, IDisposable, INavigationAware
    {
        private const int HuntingPlaceSuggestionLimit = 12;
        private readonly ClipboardMonitorService _clipboardService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly LocalEventsService _eventService;
        private readonly HuntGroupingService _groupingService;
        private readonly IHuntSessionService _huntService;
        private readonly IFileRevealService _fileRevealService;
        private readonly IImbuementCalculatorService _imbuementService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogDetectorService _logDetector;
        private readonly ILootAnalysisService _lootAnalysisService;
        private readonly INavigationService _navigationService;
        private readonly IHuntSummaryGeneratorService _summaryGeneratorService;
        private readonly IHuntSessionVerificationService _verificationService;
        private readonly ITeamHuntService _teamHuntService;
        [ObservableProperty]private string _activeCharacterName = string.Empty;
        [ObservableProperty]private ObservableCollection<HuntSupplyAdjustment> _adjustments = [];

        [ObservableProperty]private IEnumerable<string> _allItemNames = [];
        private long _cachedHourlyImbuementCost;
        [ObservableProperty]private string _calcItemName = string.Empty;
        [ObservableProperty]private int? _calcMarketPrice;
        [ObservableProperty]private int? _calcMaxDurationMinutes = 60;
        [ObservableProperty]private int? _calcUsedMinutes;

        // --- SOLO STATE ---
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EffectiveSupplies))]
        [NotifyPropertyChangedFor(nameof(EffectiveBalance))]
        [NotifyPropertyChangedFor(nameof(EffectiveXpPerHour))]
        [NotifyPropertyChangedFor(nameof(SessionImbuementCost))]
        private HuntSessionEntity? _currentSession;
        [ObservableProperty]private HuntSessionVerificationResult? _verificationResult;

        // --- NOTES ---
        [ObservableProperty]private string _currentSessionNotes = string.Empty;

        // --- TEAM STATE ---
        [ObservableProperty]private TeamHuntSessionEntity? _currentTeamSession;
        [ObservableProperty]private string _errorMessage = string.Empty;
        [ObservableProperty]private ObservableCollection<HuntingPlaceSelectionOption> _huntingPlaceOptions = [];
        [ObservableProperty]private ObservableCollection<string> _filteredHuntingPlaceSuggestions = [];
        [ObservableProperty]private string _huntingPlaceSearchText = string.Empty;
        [ObservableProperty]private HuntingPlaceSelectionOption? _selectedHuntingPlaceOption;
        [ObservableProperty]private string? _selectedHuntingPlaceSuggestion;
        [ObservableProperty]private bool _isSummaryBusy;
        [ObservableProperty]private bool _normalizeSummaryToQuarterHour;
        [ObservableProperty]private bool _showRawXpInSummary = true;
        [ObservableProperty]private ObservableCollection<SummaryFormatOption> _summaryFormatOptions = [];
        [ObservableProperty]private string _summaryPreviewText = string.Empty;
        [ObservableProperty]private ObservableCollection<SummaryTemplateOption> _summaryTemplateOptions = [];
        [ObservableProperty]private SummaryFormatOption? _selectedSummaryFormatOption;
        [ObservableProperty]private SummaryTemplateOption? _selectedSummaryTemplateOption;

        [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private bool _hasUnsavedChanges;

        // --- STATE ---
        [ObservableProperty]private string _inputText = string.Empty;
        [ObservableProperty]private bool _isAnalyzed;

        private bool _isAutoImbuementDeleted;
        [ObservableProperty]private bool _isDeduction;
        [ObservableProperty]private bool _isDoubleLoot;

        [ObservableProperty]private bool _isDoubleXp;
        [ObservableProperty]private int? _xpBoostPercent;
        [ObservableProperty]private int? _xpBoostActiveMinutes;
        [ObservableProperty]private int? _customXpRatePercent = 150;
        [ObservableProperty]private bool _isEditingNotes;
        [ObservableProperty]private bool _isHistoryMode;
        [ObservableProperty]private bool _isLoading;
        private bool _isLoadingSession;
        [ObservableProperty]private bool _isMergeResult;
        [ObservableProperty]private bool _isNotificationVisible;
        [ObservableProperty]private bool _isRapidRespawn;
        [ObservableProperty]private bool _isSummaryDialogOpen;

        [ObservableProperty]private bool _isTeamHunt;
        [ObservableProperty]private bool _isTimeBasedCalculation = true;
        [ObservableProperty]private ObservableCollection<HuntMonsterEntry> _killedMonsters = [];

        [ObservableProperty]private ObservableCollection<LootGroup> _lootGroups = [];

        [ObservableProperty]private string _newAdjustmentName = string.Empty;
        [ObservableProperty]private int? _newAdjustmentValue;

        [ObservableProperty]private string _notificationMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EffectiveXpPerHour))]
        [NotifyPropertyChangedFor(nameof(EffectiveSupplies))]
        [NotifyPropertyChangedFor(nameof(EffectiveBalance))]
        [NotifyPropertyChangedFor(nameof(SessionImbuementCost))]
        private int? _pauseMinutes;

        [ObservableProperty]private LootGroup? _selectedLootGroup;

        private List<int> _sourceSessionIds = new();
        [ObservableProperty]private ObservableCollection<BankTransferInstruction> _transfers = [];
        private bool _clipboardMonitoringActive;

        public HuntAnalyzerViewModel(
            IHuntSessionService huntService,
            ITeamHuntService teamHuntService,
            LocalEventsService eventService,
            ClipboardMonitorService clipboardService,
            ILogDetectorService logDetector,
            ILootAnalysisService lootAnalysisService,
            IHuntSessionVerificationService verificationService,
            HuntGroupingService groupingService,
            IHuntSummaryGeneratorService summaryGeneratorService,
            IImbuementCalculatorService imbuementService,
            IDbContextFactory<AppDbContext> dbFactory,
            INavigationService navigationService,
            ILocalizationService localizationService,
            IFileRevealService fileRevealService)
        {
            _huntService = huntService;
            _teamHuntService = teamHuntService;
            _eventService = eventService;
            _clipboardService = clipboardService;
            _logDetector = logDetector;
            _lootAnalysisService = lootAnalysisService;
            _verificationService = verificationService;
            _groupingService = groupingService;
            _summaryGeneratorService = summaryGeneratorService;
            _imbuementService = imbuementService;
            _dbFactory = dbFactory;
            _navigationService = navigationService;
            _localizationService = localizationService;
            _fileRevealService = fileRevealService;

            _clipboardService.LogDetected += OnLogDetected;
            _localizationService.PropertyChanged += OnLocalizationChanged;
            RefreshSummaryOptions();
            RefreshSummaryPreview();
            _ = LoadItemNamesInternal();
        }

        public long SessionImbuementCost
        {
            get
            {
                HuntSupplyAdjustment? adj = Adjustments.FirstOrDefault(a => a.Name == _localizationService["Analyzer_AutoImbuements"]);
                return adj?.Value ?? 0;
            }
        }

        public long EffectiveSupplies
        {
            get
            {
                if(CurrentSession == null)
                {
                    return 0;
                }
                long t = CurrentSession.Supplies;
                foreach(HuntSupplyAdjustment a in Adjustments)
                {
                    if(a.Type == SupplyAdjustmentType.Addition)
                    {
                        t += a.Value;
                    }
                    else
                    {
                        t -= a.Value;
                    }
                }
                return t;
            }
        }

        public long EffectiveBalance => (CurrentSession?.Loot ?? 0) - EffectiveSupplies;

        public bool HasHuntingPlaceEditor => !IsTeamHunt && CurrentSession?.Id > 0;

        public bool HasHuntingPlaceSearchText => !string.IsNullOrWhiteSpace(HuntingPlaceSearchText);

        public bool HasSummarySource => CurrentSession != null || CurrentTeamSession != null;

        public bool HasVisibleLootVerificationIssue =>
            VerificationResult?.HasLootMismatch == true &&
            CurrentSession?.IgnoreLootVerificationWarning != true;

        public bool HasVisibleXpVerificationIssue =>
            VerificationResult is not null &&
            (VerificationResult.HasRawXpMismatch || VerificationResult.HasXpMismatch) &&
            CurrentSession?.IgnoreXpVerificationWarning != true;

        public bool HasVerificationIssues => HasVisibleLootVerificationIssue || HasVisibleXpVerificationIssue;

        public bool HasRawXpVerification => HasVisibleXpVerificationIssue && VerificationResult?.ReportedRawXpGain.HasValue == true;

        public bool HasUnmatchedLootItems => HasVisibleLootVerificationIssue && VerificationResult?.UnmatchedLootItems.Count > 0;

        public bool HasUnmatchedCreatures => HasVisibleXpVerificationIssue && VerificationResult?.UnmatchedCreatures.Count > 0;

        public bool CanApplyLootVerificationCorrection => HasVisibleLootVerificationIssue && VerificationResult?.CanApplyLootCorrection == true;

        public bool CanApplyXpVerificationCorrection => HasVisibleXpVerificationIssue && VerificationResult?.CanApplyXpCorrection == true;

        public bool CanIgnoreLootVerificationWarning => HasVisibleLootVerificationIssue;

        public bool CanIgnoreXpVerificationWarning => HasVisibleXpVerificationIssue;

        public string LootVerificationText => VerificationResult == null
            ? string.Empty
            : string.Format(
                _localizationService.CurrentCulture,
                _localizationService["Analyzer_VerificationLootLine"],
                FormatNumber(VerificationResult.ReportedLoot),
                FormatNumber(VerificationResult.CalculatedLoot),
                FormatSignedDelta(VerificationResult.LootDelta));

        public string RawXpVerificationText => VerificationResult == null || !VerificationResult.ReportedRawXpGain.HasValue
            ? string.Empty
            : string.Format(
                _localizationService.CurrentCulture,
                _localizationService["Analyzer_VerificationRawXpLine"],
                FormatNumber(VerificationResult.ReportedRawXpGain.Value),
                FormatNumber(VerificationResult.CalculatedRawXpGain),
                FormatSignedDelta(VerificationResult.RawXpDelta ?? 0));

        public string XpVerificationText => VerificationResult == null
            ? string.Empty
            : string.Format(
                _localizationService.CurrentCulture,
                _localizationService[VerificationResult.IsXpEstimated
                    ? "Analyzer_VerificationXpEstimatedLine"
                    : "Analyzer_VerificationXpLine"],
                FormatNumber(VerificationResult.ReportedXpGain),
                FormatNumber(VerificationResult.CalculatedXpGain),
                FormatSignedDelta(VerificationResult.XpDelta));

        public string UnmatchedLootItemsText => VerificationResult == null || VerificationResult.UnmatchedLootItems.Count == 0
            ? string.Empty
            : string.Format(
                _localizationService.CurrentCulture,
                _localizationService["Analyzer_VerificationUnmatchedLootItems"],
                string.Join(", ", VerificationResult.UnmatchedLootItems));

        public string UnmatchedCreaturesText => VerificationResult == null || VerificationResult.UnmatchedCreatures.Count == 0
            ? string.Empty
            : string.Format(
                _localizationService.CurrentCulture,
                _localizationService["Analyzer_VerificationUnmatchedCreatures"],
                string.Join(", ", VerificationResult.UnmatchedCreatures));

        public string SummaryActionLabel => SelectedSummaryFormatOption?.Format == HuntSummaryFormat.Image
            ? _localizationService["Analyzer_SummaryExportImage"]
            : _localizationService["Analyzer_SummaryCopy"];

        public long EffectiveXpPerHour
        {
            get
            {
                if(CurrentSession == null || CurrentSession.Duration.TotalHours <= 0)
                {
                    return 0;
                }
                double real = CurrentSession.Duration.TotalHours - (PauseMinutes ?? 0) / 60.0;
                return real <= 0.01 ? 0 : (long)(CurrentSession.XpGain / real);
            }
        }

        public void Dispose()
        {
            if(_clipboardMonitoringActive)
            {
                _clipboardService.Stop();
                _clipboardMonitoringActive = false;
            }

            _clipboardService.LogDetected -= OnLogDetected;
            _localizationService.PropertyChanged -= OnLocalizationChanged;
        }

        // INavigationAware Implementation
        public void OnNavigatedTo(object? parameter)
        {
            if(!_clipboardMonitoringActive)
            {
                _clipboardService.Start();
                _clipboardMonitoringActive = true;
            }

            if(parameter is AnalyzerWithCharacter charParam)
            {
                ActiveCharacterName = charParam.CharacterName;
            }
            else if(parameter is AnalyzerWithSession sessionParam)
            {
                ActiveCharacterName = sessionParam.CharacterName;
                _ = LoadExistingSessionAsync(sessionParam.Session, sessionParam.SourceIds);
            }
            else if(parameter is AnalyzerWithTeamSession teamParam)
            {
                ActiveCharacterName = teamParam.CharacterName;
                LoadTeamSession(teamParam.TeamSession);
            }
        }

        public void OnNavigatedFrom()
        {
            if(_clipboardMonitoringActive)
            {
                _clipboardService.Stop();
                _clipboardMonitoringActive = false;
            }
        }

        // Removed events - using NavigationService instead

        [RelayCommand]
        private async Task AnalyzeAndSave()
        {
            if(string.IsNullOrWhiteSpace(InputText) || string.IsNullOrEmpty(ActiveCharacterName))
            {
                return;
            }
            ErrorMessage = string.Empty;

            if(UserInputSanitizer.ExceedsLength(InputText, UserInputLimits.HuntLogMaxLength))
            {
                ErrorMessage = $"Input is too large (max {UserInputLimits.HuntLogMaxLength} characters).";
                return;
            }

            string safeInputText = UserInputSanitizer.Truncate(InputText, UserInputLimits.HuntLogMaxLength);
            DetectedLogType logType = _logDetector.DetectType(safeInputText);

            if(logType == DetectedLogType.TeamHunt)
            {
                IsTeamHunt = true;
                (SessionImportResult result, TeamHuntSessionEntity? session, string? errorDetail) = await _teamHuntService.ImportTeamSessionAsync(safeInputText, ActiveCharacterName);
                if(result == SessionImportResult.Success && session != null)
                {
                    CurrentTeamSession = session;
                    CalculatePartySplit();
                    IsAnalyzed = true;
                }
                else
                {
                    ErrorMessage = errorDetail ?? _localizationService["Analyzer_ErrorTeamHuntParse"];
                }
            }
            else
            {
                IsTeamHunt = false;
                SessionImportOptions options = new(
                    safeInputText,
                    ActiveCharacterName,
                    IsDoubleXp,
                    IsDoubleLoot,
                    IsRapidRespawn,
                    null,
                    XpBoostPercent,
                    XpBoostActiveMinutes,
                    CustomXpRatePercent);
                (SessionImportResult result, HuntSessionEntity? session, string? errorDetail) = await _huntService.ImportSessionAsync(options);

                switch (result)
                {
                    case SessionImportResult.Success:
                        if(session != null)
                        {
                            await LoadSessionInternal(session);
                        }
                        break;
                    case SessionImportResult.ParseError:
                        ErrorMessage = string.Format(_localizationService["Analyzer_ErrorLogParse"], errorDetail);
                        break;
                    case SessionImportResult.Duplicate:
                        ErrorMessage = _localizationService["Analyzer_ErrorDuplicate"];
                        break;
                    case SessionImportResult.CharacterNotFound:
                        ErrorMessage = _localizationService["Analyzer_ErrorCharacterNotFound"];
                        break;
                }
            }
        }

        // --- NOTES LOGIC ---

        [RelayCommand]
        private void ToggleEditNotes()
        {
            IsEditingNotes = !IsEditingNotes;
            if(IsEditingNotes)
            {
                // Lade existierende Notizen in das Bearbeitungsfeld
                if(IsTeamHunt)
                {
                    CurrentSessionNotes = CurrentTeamSession?.Notes ?? string.Empty;
                }
                else
                {
                    CurrentSessionNotes = CurrentSession?.Notes ?? string.Empty;
                }
            }
        }

        [RelayCommand]
        private async Task SaveNotes()
        {
            string? normalizedNotes = UserInputSanitizer.TrimAndTruncateOrNull(CurrentSessionNotes, UserInputLimits.SessionNotesMaxLength);
            CurrentSessionNotes = normalizedNotes ?? string.Empty;

            // Speichern und UI aktualisieren
            if(IsTeamHunt)
            {
                if(CurrentTeamSession == null || CurrentTeamSession.Id == 0)
                {
                    return;
                }

                CurrentTeamSession.Notes = normalizedNotes;
                await _teamHuntService.UpdateSessionAsync(CurrentTeamSession);

                TeamHuntSessionEntity? temp = CurrentTeamSession;
                CurrentTeamSession = null;
                CurrentTeamSession = temp;
            }
            else
            {
                if(CurrentSession == null || CurrentSession.Id == 0)
                {
                    return;
                }

                CurrentSession.Notes = normalizedNotes;
                await _huntService.UpdateSessionAsync(CurrentSession);

                // Trigger UI Update
                HuntSessionEntity? temp = CurrentSession;
                CurrentSession = null;
                CurrentSession = temp;
            }

            IsEditingNotes = false;
            NotificationMessage = _localizationService["Analyzer_NotesSaved"];
            IsNotificationVisible = true;
            await Task.Delay(2000);
            IsNotificationVisible = false;
        }

        // --- TEAM LOGIC ---
        private void CalculatePartySplit()
        {
            if(CurrentTeamSession == null || CurrentTeamSession.Members.Count == 0)
            {
                RefreshSummaryPreview();
                return;
            }
            Transfers.Clear();
            long fairShare = CurrentTeamSession.TotalBalance / CurrentTeamSession.Members.Count;
            List<(TeamHuntMemberEntity Member, long Debt)> payers = new();
            List<(TeamHuntMemberEntity Member, long Credit)> receivers = new();

            foreach(TeamHuntMemberEntity m in CurrentTeamSession.Members)
            {
                long diff = fairShare - m.Balance;
                if(diff < 0)
                {
                    payers.Add((m, Math.Abs(diff)));
                }
                else if(diff > 0)
                {
                    receivers.Add((m, diff));
                }
            }

            int pIndex = 0, rIndex = 0;
            while (pIndex < payers.Count && rIndex < receivers.Count)
            {
                (TeamHuntMemberEntity Member, long Debt) payer = payers[pIndex];
                (TeamHuntMemberEntity Member, long Credit) receiver = receivers[rIndex];
                long amount = Math.Min(payer.Debt, receiver.Credit);
                if(amount > 0)
                {
                    Transfers.Add(new BankTransferInstruction(payer.Member.Name, receiver.Member.Name, amount));
                }

                payers[pIndex] = (payer.Member, payer.Debt - amount);
                receivers[rIndex] = (receiver.Member, receiver.Credit - amount);
                if(payers[pIndex].Debt == 0)
                {
                    pIndex++;
                }
                if(receivers[rIndex].Credit == 0)
                {
                    rIndex++;
                }
            }

            RefreshSummaryPreview();
        }

        public void LoadTeamSession(TeamHuntSessionEntity session)
        {
            Reset();
            IsHistoryMode = true;
            IsTeamHunt = true;
            CurrentTeamSession = session;
            CalculatePartySplit();
            IsAnalyzed = true;
        }

        // --- SOLO LOGIC ---
        public async Task LoadExistingSessionAsync(HuntSessionEntity session, List<int>? sourceIds = null)
        {
            Reset();
            IsHistoryMode = true;
            IsTeamHunt = false;
            if(sourceIds is { Count: > 1 })
            {
                _sourceSessionIds = sourceIds;
                IsMergeResult = true;
            }
            await LoadSessionInternal(session);
        }

        private async Task LoadSessionInternal(HuntSessionEntity session)
        {
            _isLoadingSession = true;
            IsLoading = true;
            try
            {
                CurrentSession = session;
                PauseMinutes = 0;
                IsDoubleXp = session.IsDoubleXp;
                XpBoostPercent = session.XpBoostPercent;
                XpBoostActiveMinutes = session.XpBoostActiveMinutes;
                CustomXpRatePercent = session.CustomXpRatePercent ?? 150;
                CurrentSession.CustomXpRatePercent = CustomXpRatePercent;
                _isAutoImbuementDeleted = false;

                if(session.KilledMonsters != null)
                {
                    KilledMonsters = new ObservableCollection<HuntMonsterEntry>(session.KilledMonsters.OrderByDescending(m => m.Amount));
                }
                if(session.SupplyAdjustments != null)
                {
                    Adjustments = new ObservableCollection<HuntSupplyAdjustment>(session.SupplyAdjustments);
                }
                if(session.LootItems != null)
                {
                    List<LootGroup> groups = await _lootAnalysisService.AnalyzeLootListAsync(session.LootItems);
                    LootGroups = new ObservableCollection<LootGroup>(groups);
                    SelectedLootGroup = LootGroups.FirstOrDefault();
                }

                await EnsureHuntingPlaceOptionsLoadedAsync();
                SyncSelectedHuntingPlace(session.HuntingPlaceId);
                await RefreshVerificationAsync();

                bool hasAutoImbue = Adjustments.Any(a => a.Name == _localizationService["Analyzer_AutoImbuements"]);
                if(session.Id == 0)
                {
                    await RefreshImbuementCosts(false);
                }
                else
                {
                    if(!hasAutoImbue)
                    {
                        _isAutoImbuementDeleted = true;
                    }
                    _cachedHourlyImbuementCost = await _imbuementService.CalculateHourlyCostAsync(CurrentSession.CharacterId);
                }
                IsAnalyzed = true;
                HasUnsavedChanges = false;
            }
            finally
            {
                _isLoadingSession = false;
                IsLoading = false;
                NotifyRecalculation();
            }
        }

        public async Task RefreshImbuementCosts(bool markAsDirty = true)
        {
            if(CurrentSession == null)
            {
                return;
            }
            _cachedHourlyImbuementCost = await _imbuementService.CalculateHourlyCostAsync(CurrentSession.CharacterId);
            double totalMinutes = CurrentSession.Duration.TotalMinutes - (PauseMinutes ?? 0);
            long totalCost = 0;
            if(totalMinutes > 0 && _cachedHourlyImbuementCost > 0)
            {
                totalCost = (long)(totalMinutes / 60.0 * _cachedHourlyImbuementCost);
            }
            HuntSupplyAdjustment? existingAdj = Adjustments.FirstOrDefault(a => a.Name == _localizationService["Analyzer_AutoImbuements"]);
            if(_isAutoImbuementDeleted && existingAdj == null)
            {
                NotifyRecalculation();
                return;
            }
            bool changed = false;
            if(totalCost > 0)
            {
                if(existingAdj == null)
                {
                    Adjustments.Add(new HuntSupplyAdjustment
                    {
                        Name = _localizationService["Analyzer_AutoImbuements"],
                        Value = totalCost,
                        Type = SupplyAdjustmentType.Addition
                    });
                    changed = true;
                }
                else if(existingAdj.Value != totalCost)
                {
                    existingAdj.Value = totalCost;
                    int idx = Adjustments.IndexOf(existingAdj);
                    Adjustments[idx] = existingAdj;
                    changed = true;
                }
            }
            else if(existingAdj != null)
            {
                Adjustments.Remove(existingAdj);
                changed = true;
            }
            if(markAsDirty && changed)
            {
                HasUnsavedChanges = true;
            }
            NotifyRecalculation();
        }

        [RelayCommand(CanExecute = nameof(CanSaveChanges))]
        private async Task SaveChanges()
        {
            if(CurrentSession == null || CurrentSession.Id == 0)
            {
                return;
            }
            try
            {
                CurrentSession.IsDoubleXp = IsDoubleXp;
                CurrentSession.XpBoostPercent = XpBoostPercent;
                CurrentSession.XpBoostActiveMinutes = XpBoostActiveMinutes;
                CurrentSession.CustomXpRatePercent = CustomXpRatePercent;
                await _huntService.UpdateSessionAsync(CurrentSession);
                await _huntService.ReplacedAdjustmentAsync(CurrentSession.Id, Adjustments.ToList());
                HasUnsavedChanges = false;
                NotificationMessage = _localizationService["Analyzer_ChangesSaved"];
                IsNotificationVisible = true;
                await Task.Delay(2000);
                IsNotificationVisible = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        [RelayCommand]
        private async Task ApplyLootCorrection()
        {
            if(CurrentSession == null || VerificationResult == null || !VerificationResult.CanApplyLootCorrection)
            {
                return;
            }

            CurrentSession.Loot = VerificationResult.CalculatedLoot;
            CurrentSession.IgnoreLootVerificationWarning = false;
            HasUnsavedChanges = true;
            NotifyRecalculation();
            await RefreshVerificationAsync();
            await ShowSummaryNotificationAsync(_localizationService["Analyzer_VerificationCorrectionStaged"]);
        }

        [RelayCommand]
        private async Task ApplyXpCorrection()
        {
            if(CurrentSession == null || VerificationResult == null || !VerificationResult.CanApplyXpCorrection)
            {
                return;
            }

            if(CurrentSession.RawXpGain.HasValue)
            {
                CurrentSession.RawXpGain = VerificationResult.CalculatedRawXpGain;
            }

            CurrentSession.XpGain = VerificationResult.CalculatedXpGain;
            CurrentSession.XpPerHour = VerificationResult.CalculatedXpPerHour;
            CurrentSession.IgnoreXpVerificationWarning = false;
            HasUnsavedChanges = true;
            NotifyRecalculation();
            await RefreshVerificationAsync();
            await ShowSummaryNotificationAsync(_localizationService["Analyzer_VerificationCorrectionStaged"]);
        }

        [RelayCommand]
        private async Task IgnoreLootVerificationWarning()
        {
            if(CurrentSession == null || !CanIgnoreLootVerificationWarning)
            {
                return;
            }

            CurrentSession.IgnoreLootVerificationWarning = true;
            HasUnsavedChanges = true;
            OnVerificationResultChanged(VerificationResult);
            await ShowSummaryNotificationAsync(_localizationService["Analyzer_VerificationIgnored"]);
        }

        [RelayCommand]
        private async Task IgnoreXpVerificationWarning()
        {
            if(CurrentSession == null || !CanIgnoreXpVerificationWarning)
            {
                return;
            }

            CurrentSession.IgnoreXpVerificationWarning = true;
            HasUnsavedChanges = true;
            OnVerificationResultChanged(VerificationResult);
            await ShowSummaryNotificationAsync(_localizationService["Analyzer_VerificationIgnored"]);
        }
        private bool CanSaveChanges()
        {
            return HasUnsavedChanges && CurrentSession?.Id > 0;
        }

        [RelayCommand]
        private async Task SaveHuntingPlace()
        {
            if(CurrentSession == null || CurrentSession.Id <= 0)
            {
                return;
            }

            if(!TryResolveHuntingPlaceSelection(out HuntingPlaceSelectionOption? resolvedOption))
            {
                ErrorMessage = _localizationService["Analyzer_HuntingPlaceChooseSpecific"];
                return;
            }

            int? selectedId = resolvedOption?.Id;
            if(CurrentSession.HuntingPlaceId == selectedId)
            {
                return;
            }

            try
            {
                CurrentSession.HuntingPlaceId = selectedId;
                await _huntService.UpdateSessionAsync(CurrentSession);
                SelectedHuntingPlaceOption = resolvedOption;
                SelectedHuntingPlaceSuggestion = resolvedOption?.DisplayName;
                HuntingPlaceSearchText = resolvedOption?.DisplayName ?? string.Empty;

                NotificationMessage = selectedId.HasValue
                    ? _localizationService["Analyzer_HuntingPlaceSaved"]
                    : _localizationService["Analyzer_HuntingPlaceCleared"];
                IsNotificationVisible = true;
                await Task.Delay(2000);
                IsNotificationVisible = false;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        [RelayCommand]
        private void ClearHuntingPlaceSearch()
        {
            HuntingPlaceSearchText = string.Empty;
            SelectedHuntingPlaceSuggestion = null;
            SelectedHuntingPlaceOption = null;
        }

        [RelayCommand]
        private void DeleteAdjustment(HuntSupplyAdjustment adj)
        {
            Adjustments.Remove(adj);
            if(adj.Name == _localizationService["Analyzer_AutoImbuements"])
            {
                _isAutoImbuementDeleted = true;
            }
            HasUnsavedChanges = true;
            NotifyRecalculation();
        }
        [RelayCommand]
        private void OpenImbuementConfig()
        {
            if(CurrentSession != null)
            {
                _isAutoImbuementDeleted = false;
                _navigationService.NavigateTo<ImbuementConfigurationViewModel>(
                    new ImbuementConfiguration(CurrentSession.CharacterId, OnImbuementConfigClosed)
                );
            }
        }

        private void OnImbuementConfigClosed()
        {
            // Refresh imbuement costs after closing the config
            _ = RefreshImbuementCosts();
        }

        [RelayCommand]
        private void AddSmartSupply()
        {
            string safeItemName = UserInputSanitizer.TrimAndTruncate(CalcItemName, UserInputLimits.HuntAdjustmentNameMaxLength);
            if(string.IsNullOrWhiteSpace(safeItemName))
            {
                return;
            }
            long finalCost = 0;
            if(IsTimeBasedCalculation)
            {
                if(CalcMarketPrice > 0 && CalcMaxDurationMinutes > 0 && CalcUsedMinutes > 0)
                {
                    finalCost = (long)((double)CalcMarketPrice / CalcMaxDurationMinutes.Value * CalcUsedMinutes.Value);
                }
            }
            else
            {
                finalCost = CalcMarketPrice ?? 0;
            }
            if(finalCost <= 0)
            {
                return;
            }
            Adjustments.Add(new HuntSupplyAdjustment
            {
                Name = UserInputSanitizer.TrimAndTruncate(
                    IsTimeBasedCalculation
                        ? string.Format(_localizationService["Analyzer_SmartSupplyFormat"], safeItemName, CalcUsedMinutes)
                        : safeItemName,
                    UserInputLimits.HuntAdjustmentNameMaxLength),
                Value = finalCost,
                Type = SupplyAdjustmentType.Addition
            });
            CalcItemName = string.Empty;
            CalcMarketPrice = null;
            HasUnsavedChanges = true;
            NotifyRecalculation();
        }

        [RelayCommand]
        private void AddAdjustment()
        {
            string safeAdjustmentName = UserInputSanitizer.TrimAndTruncate(NewAdjustmentName, UserInputLimits.HuntAdjustmentNameMaxLength);
            if(string.IsNullOrWhiteSpace(safeAdjustmentName) || NewAdjustmentValue <= 0)
            {
                return;
            }
            Adjustments.Add(new HuntSupplyAdjustment
            {
                Name = safeAdjustmentName,
                Value = NewAdjustmentValue ?? 0,
                Type = IsDeduction ? SupplyAdjustmentType.Deduction : SupplyAdjustmentType.Addition
            });
            NewAdjustmentName = string.Empty;
            NewAdjustmentValue = null;
            HasUnsavedChanges = true;
            NotifyRecalculation();
        }

        partial void OnPauseMinutesChanged(int? value)
        {
            if(!_isLoadingSession)
            {
                _ = RefreshImbuementCosts();
            }
        }

        private void NotifyRecalculation()
        {
            OnPropertyChanged(nameof(EffectiveSupplies));
            OnPropertyChanged(nameof(EffectiveBalance));
            OnPropertyChanged(nameof(EffectiveXpPerHour));
            OnPropertyChanged(nameof(SessionImbuementCost));
            RefreshSummaryPreview();
        }
        private async Task LoadItemNamesInternal()
        {
            try
            {
                await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
                AllItemNames = await db.Items.AsNoTracking().Select(i => i.Name).OrderBy(n => n).ToListAsync();
            }
            catch
            {
            }
        }
        [RelayCommand]
        private void UseSessionDurationForCalc()
        {
            if(CurrentSession != null)
            {
                CalcUsedMinutes = (int)Math.Max(0, CurrentSession.Duration.TotalMinutes - (PauseMinutes ?? 0));
            }
        }
        [RelayCommand]
        private void GoBack()
        {
            // Navigate back to History
            if(!string.IsNullOrEmpty(ActiveCharacterName))
            {
                _navigationService.NavigateTo<HistoryViewModel>(
                    new HistoryWithCharacter(ActiveCharacterName)
                );
            }
        }
        [RelayCommand]
        private void Reset()
        {
            IsAnalyzed = false;
            IsHistoryMode = false;
            IsMergeResult = false;
            HasUnsavedChanges = false;
            IsTeamHunt = false;
            IsSummaryDialogOpen = false;
            CurrentTeamSession = null;
            InputText = string.Empty;
            CurrentSession = null;
            VerificationResult = null;
            IsDoubleXp = false;
            IsDoubleLoot = false;
            IsRapidRespawn = false;
            XpBoostPercent = null;
            XpBoostActiveMinutes = null;
            CustomXpRatePercent = 150;
            Adjustments.Clear();
            SelectedHuntingPlaceOption = null;
            SelectedHuntingPlaceSuggestion = null;
            HuntingPlaceSearchText = string.Empty;
            FilteredHuntingPlaceSuggestions.Clear();
            ErrorMessage = string.Empty;
            SummaryPreviewText = string.Empty;
        }
        private void OnLogDetected(string text, DetectedLogType type)
        {
            if(IsAnalyzed)
            {
                return;
            }
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                InputText = text;
                NotificationMessage = _localizationService["Analyzer_LogDetected"];
                IsNotificationVisible = true;
                await Task.Delay(3000);
                IsNotificationVisible = false;
            });
        }
        [RelayCommand]
        private async Task PasteFromClipboard()
        {
            if(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                IClipboard? clipboard = desktop.MainWindow?.Clipboard;
                if(clipboard != null)
                {
                    string? text = await clipboard.TryGetTextAsync();
                    if(!string.IsNullOrWhiteSpace(text))
                    {
                        InputText = text;
                    }
                }
            }
        }
        [RelayCommand]
        private async Task SaveMerge()
        {
            if(!_sourceSessionIds.Any())
            {
                return;
            }
            try
            {
                await _groupingService.CreateGroupAsync(string.Format(_localizationService["Analyzer_MergedHuntName"], DateTime.Now.ToString("g")), _sourceSessionIds);
                GoBack();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
        [RelayCommand]
        private async Task CopyTransfer(string text)
        {
            if(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                IClipboard? clipboard = desktop.MainWindow?.Clipboard;
                if(clipboard != null)
                {
                    await clipboard.SetTextAsync(text);
                }
            }
        }

        [RelayCommand]
        private async Task ExecuteSummaryAction()
        {
            HuntSummaryRequest? request = BuildSummaryRequest();
            if(request == null)
            {
                return;
            }

            if(IsSummaryBusy)
            {
                return;
            }

            if(Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            IsSummaryBusy = true;
            IClipboard? clipboard = desktop.MainWindow?.Clipboard;
            try
            {
                if(request.Format == HuntSummaryFormat.Image)
                {
                    string filePath = await _summaryGeneratorService.ExportImageAsync(request);
                    await _fileRevealService.RevealFileAsync(filePath);
                    await ShowSummaryNotificationAsync(_localizationService["Analyzer_SummaryImageExported"]);
                }
                else
                {
                    if(clipboard == null)
                    {
                        return;
                    }

                    string summary = _summaryGeneratorService.BuildText(request);
                    await clipboard.SetTextAsync(summary);
                    await ShowSummaryNotificationAsync(_localizationService["Analyzer_SummaryCopied"]);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = string.Format(_localizationService["Analyzer_SummaryActionFailed"], ex.Message);
            }
            finally
            {
                IsSummaryBusy = false;
            }
        }

        [RelayCommand]
        private void OpenSummaryDialog()
        {
            if(!HasSummarySource)
            {
                return;
            }

            RefreshSummaryPreview();
            IsSummaryDialogOpen = true;
        }

        [RelayCommand]
        private void CloseSummaryDialog()
        {
            IsSummaryDialogOpen = false;
        }

        partial void OnCurrentSessionChanged(HuntSessionEntity? value)
        {
            OnPropertyChanged(nameof(HasHuntingPlaceEditor));
            OnPropertyChanged(nameof(HasSummarySource));
            OnVerificationResultChanged(VerificationResult);
            RefreshSummaryPreview();
        }

        partial void OnVerificationResultChanged(HuntSessionVerificationResult? value)
        {
            OnPropertyChanged(nameof(HasVisibleLootVerificationIssue));
            OnPropertyChanged(nameof(HasVisibleXpVerificationIssue));
            OnPropertyChanged(nameof(HasVerificationIssues));
            OnPropertyChanged(nameof(HasRawXpVerification));
            OnPropertyChanged(nameof(HasUnmatchedLootItems));
            OnPropertyChanged(nameof(HasUnmatchedCreatures));
            OnPropertyChanged(nameof(CanApplyLootVerificationCorrection));
            OnPropertyChanged(nameof(CanApplyXpVerificationCorrection));
            OnPropertyChanged(nameof(CanIgnoreLootVerificationWarning));
            OnPropertyChanged(nameof(CanIgnoreXpVerificationWarning));
            OnPropertyChanged(nameof(LootVerificationText));
            OnPropertyChanged(nameof(RawXpVerificationText));
            OnPropertyChanged(nameof(XpVerificationText));
            OnPropertyChanged(nameof(UnmatchedLootItemsText));
            OnPropertyChanged(nameof(UnmatchedCreaturesText));
        }

        partial void OnIsDoubleXpChanged(bool value)
        {
            OnSoloXpModifiersChanged();
        }

        partial void OnXpBoostPercentChanged(int? value)
        {
            OnSoloXpModifiersChanged();
        }

        partial void OnXpBoostActiveMinutesChanged(int? value)
        {
            OnSoloXpModifiersChanged();
        }

        partial void OnCustomXpRatePercentChanged(int? value)
        {
            OnSoloXpModifiersChanged();
        }

        partial void OnIsTeamHuntChanged(bool value)
        {
            OnPropertyChanged(nameof(HasHuntingPlaceEditor));
            OnPropertyChanged(nameof(HasSummarySource));
            RefreshSummaryPreview();
        }

        partial void OnHuntingPlaceSearchTextChanged(string value)
        {
            OnPropertyChanged(nameof(HasHuntingPlaceSearchText));
            RefreshSummaryPreview();

            if(SelectedHuntingPlaceOption != null &&
               !string.IsNullOrWhiteSpace(SelectedHuntingPlaceSuggestion) &&
               string.Equals(value, SelectedHuntingPlaceSuggestion, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            UpdateHuntingPlaceSuggestions(value);
        }

        partial void OnSelectedHuntingPlaceSuggestionChanged(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            HuntingPlaceSelectionOption? match = HuntingPlaceOptions.FirstOrDefault(option =>
                string.Equals(option.DisplayName, value, StringComparison.OrdinalIgnoreCase));

            if(match == null)
            {
                return;
            }

            SelectedHuntingPlaceOption = match;
            if(!string.Equals(HuntingPlaceSearchText, match.DisplayName, StringComparison.Ordinal))
            {
                HuntingPlaceSearchText = match.DisplayName;
            }

            if(CurrentSession?.Id > 0 && CurrentSession.HuntingPlaceId != match.Id)
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await SaveHuntingPlace();
                });
            }
        }

        partial void OnCurrentTeamSessionChanged(TeamHuntSessionEntity? value)
        {
            OnPropertyChanged(nameof(HasSummarySource));
            RefreshSummaryPreview();
        }

        partial void OnNormalizeSummaryToQuarterHourChanged(bool value)
        {
            RefreshSummaryPreview();
        }

        partial void OnSelectedSummaryFormatOptionChanged(SummaryFormatOption? value)
        {
            OnPropertyChanged(nameof(SummaryActionLabel));
            RefreshSummaryPreview();
        }

        partial void OnSelectedSummaryTemplateOptionChanged(SummaryTemplateOption? value)
        {
            RefreshSummaryPreview();
        }

        partial void OnShowRawXpInSummaryChanged(bool value)
        {
            RefreshSummaryPreview();
        }

        private async Task EnsureHuntingPlaceOptionsLoadedAsync()
        {
            if(HuntingPlaceOptions.Count > 0)
            {
                return;
            }

            await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
            List<HuntingPlaceSelectionOption> places = await db.HuntingPlaces
                                                               .AsNoTracking()
                                                               .OrderBy(p => p.Name)
                                                               .Select(p => new HuntingPlaceSelectionOption(p.Id, p.Name))
                                                               .ToListAsync();

            HuntingPlaceOptions = new ObservableCollection<HuntingPlaceSelectionOption>(places);
            UpdateHuntingPlaceSuggestions(HuntingPlaceSearchText);
        }

        private void SyncSelectedHuntingPlace(int? huntingPlaceId)
        {
            if(HuntingPlaceOptions.Count == 0)
            {
                SelectedHuntingPlaceOption = null;
                SelectedHuntingPlaceSuggestion = null;
                HuntingPlaceSearchText = string.Empty;
                return;
            }

            SelectedHuntingPlaceOption = HuntingPlaceOptions.FirstOrDefault(x => x.Id == huntingPlaceId);
            SelectedHuntingPlaceSuggestion = SelectedHuntingPlaceOption?.DisplayName;
            HuntingPlaceSearchText = SelectedHuntingPlaceOption?.DisplayName ?? string.Empty;
            UpdateHuntingPlaceSuggestions(HuntingPlaceSearchText);
        }

        private void UpdateHuntingPlaceSuggestions(string rawInput)
        {
            string query = rawInput?.Trim() ?? string.Empty;
            IEnumerable<HuntingPlaceSelectionOption> matches = string.IsNullOrWhiteSpace(query)
                ? HuntingPlaceOptions.Take(HuntingPlaceSuggestionLimit)
                : HuntingPlaceOptions
                    .Where(option => option.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(option => option.DisplayName.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(option => option.DisplayName.Length)
                    .ThenBy(option => option.DisplayName)
                    .Take(HuntingPlaceSuggestionLimit);

            ReplaceFilteredHuntingPlaceSuggestions(matches.Select(option => option.DisplayName));

            if(string.IsNullOrWhiteSpace(query))
            {
                SelectedHuntingPlaceOption = null;
                SelectedHuntingPlaceSuggestion = null;
                return;
            }

            HuntingPlaceSelectionOption? exactMatch = HuntingPlaceOptions.FirstOrDefault(option =>
                string.Equals(option.DisplayName, query, StringComparison.OrdinalIgnoreCase));

            if(exactMatch != null)
            {
                SelectedHuntingPlaceOption = exactMatch;
                SelectedHuntingPlaceSuggestion = exactMatch.DisplayName;
                return;
            }

            if(SelectedHuntingPlaceOption != null &&
               !string.Equals(SelectedHuntingPlaceOption.DisplayName, query, StringComparison.OrdinalIgnoreCase))
            {
                SelectedHuntingPlaceOption = null;
            }
        }

        private bool TryResolveHuntingPlaceSelection(out HuntingPlaceSelectionOption? selection)
        {
            string query = HuntingPlaceSearchText?.Trim() ?? string.Empty;
            if(string.IsNullOrWhiteSpace(query))
            {
                selection = null;
                return true;
            }

            if(SelectedHuntingPlaceOption != null &&
               string.Equals(SelectedHuntingPlaceOption.DisplayName, query, StringComparison.OrdinalIgnoreCase))
            {
                selection = SelectedHuntingPlaceOption;
                return true;
            }

            List<HuntingPlaceSelectionOption> exactMatches = HuntingPlaceOptions
                .Where(option => string.Equals(option.DisplayName, query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if(exactMatches.Count == 1)
            {
                selection = exactMatches[0];
                return true;
            }

            List<HuntingPlaceSelectionOption> prefixMatches = HuntingPlaceOptions
                .Where(option => option.DisplayName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if(prefixMatches.Count == 1)
            {
                selection = prefixMatches[0];
                return true;
            }

            List<HuntingPlaceSelectionOption> containsMatches = HuntingPlaceOptions
                .Where(option => option.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if(containsMatches.Count == 1)
            {
                selection = containsMatches[0];
                return true;
            }

            selection = null;
            return false;
        }

        private void ReplaceFilteredHuntingPlaceSuggestions(IEnumerable<string> items)
        {
            FilteredHuntingPlaceSuggestions.Clear();
            foreach(string item in items)
            {
                FilteredHuntingPlaceSuggestions.Add(item);
            }
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            RefreshSummaryOptions();
            RefreshSummaryPreview();
            OnVerificationResultChanged(VerificationResult);
        }

        private void RefreshSummaryOptions()
        {
            HuntSummaryFormat selectedFormat = SelectedSummaryFormatOption?.Format ?? HuntSummaryFormat.Discord;
            HuntSummaryTemplatePreset selectedPreset = SelectedSummaryTemplateOption?.Preset ?? HuntSummaryTemplatePreset.Compact;

            SummaryFormatOptions =
            [
                new SummaryFormatOption(HuntSummaryFormat.Discord, _localizationService["Analyzer_SummaryFormatDiscord"]),
                new SummaryFormatOption(HuntSummaryFormat.Text, _localizationService["Analyzer_SummaryFormatText"]),
                new SummaryFormatOption(HuntSummaryFormat.Image, _localizationService["Analyzer_SummaryFormatImage"])
            ];

            SummaryTemplateOptions =
            [
                new SummaryTemplateOption(HuntSummaryTemplatePreset.Compact, _localizationService["Analyzer_SummaryPresetCompact"]),
                new SummaryTemplateOption(HuntSummaryTemplatePreset.Detailed, _localizationService["Analyzer_SummaryPresetDetailed"])
            ];

            SelectedSummaryFormatOption = SummaryFormatOptions.First(x => x.Format == selectedFormat);
            SelectedSummaryTemplateOption = SummaryTemplateOptions.First(x => x.Preset == selectedPreset);
            OnPropertyChanged(nameof(SummaryActionLabel));
        }

        private void RefreshSummaryPreview()
        {
            HuntSummaryRequest? request = BuildSummaryRequest();
            SummaryPreviewText = request == null
                ? string.Empty
                : _summaryGeneratorService.BuildPreviewText(request);
        }

        private HuntSummaryRequest? BuildSummaryRequest()
        {
            HuntSummaryFormat format = SelectedSummaryFormatOption?.Format ?? HuntSummaryFormat.Discord;
            HuntSummaryTemplatePreset preset = SelectedSummaryTemplateOption?.Preset ?? HuntSummaryTemplatePreset.Compact;
            TimeSpan? effectiveDuration = CurrentSession != null
                ? TimeSpan.FromMinutes(Math.Max(0, CurrentSession.Duration.TotalMinutes - (PauseMinutes ?? 0)))
                : null;
            List<HuntSummaryTransfer> transfers = Transfers
                .Select(x => new HuntSummaryTransfer(x.FromName, x.ToName, x.Amount))
                .ToList();

            if(IsTeamHunt)
            {
                if(CurrentTeamSession == null)
                {
                    return null;
                }

            return new HuntSummaryRequest(
                ActiveCharacterName,
                format,
                preset,
                NormalizeSummaryToQuarterHour,
                ShowRawXpInSummary,
                null,
                CurrentTeamSession,
                transfers,
                null,
                CurrentTeamSession.XpPerHour,
                CurrentTeamSession.TotalBalance,
                CurrentTeamSession.TotalSupplies,
                0,
                HuntSummaryGeneratorService.TryExtractRawXpGain(CurrentTeamSession.RawInput),
                CurrentTeamSession.Duration);
        }

            if(CurrentSession == null)
            {
                return null;
            }

            string? huntingPlaceName = string.IsNullOrWhiteSpace(HuntingPlaceSearchText)
                ? null
                : HuntingPlaceSearchText.Trim();

            return new HuntSummaryRequest(
                ActiveCharacterName,
                format,
                preset,
                NormalizeSummaryToQuarterHour,
                ShowRawXpInSummary,
                CurrentSession,
                null,
                transfers,
                huntingPlaceName,
                EffectiveXpPerHour,
                EffectiveBalance,
                EffectiveSupplies,
                SessionImbuementCost,
                CurrentSession.RawXpGain ?? HuntSummaryGeneratorService.TryExtractRawXpGain(CurrentSession.RawInput),
                effectiveDuration,
                XpBoostPercent,
                XpBoostActiveMinutes,
                CustomXpRatePercent,
                !IsMergeResult);
        }

        private void OnSoloXpModifiersChanged()
        {
            if(!_isLoadingSession && CurrentSession?.Id > 0 && !IsTeamHunt)
            {
                HasUnsavedChanges = true;
                _ = RefreshVerificationAsync();
            }

            RefreshSummaryPreview();
        }

        private async Task RefreshVerificationAsync()
        {
            if(IsTeamHunt || CurrentSession == null)
            {
                VerificationResult = null;
                return;
            }

            try
            {
                VerificationResult = await _verificationService.VerifyAsync(CurrentSession);
            }
            catch
            {
                VerificationResult = null;
            }
        }

        private string FormatNumber(long value)
        {
            return value.ToString("N0", _localizationService.CurrentCulture);
        }

        private string FormatSignedDelta(long value)
        {
            return value >= 0
                ? $"+{FormatNumber(value)}"
                : value.ToString("N0", _localizationService.CurrentCulture);
        }

        private async Task ShowSummaryNotificationAsync(string message)
        {
            NotificationMessage = message;
            IsNotificationVisible = true;
            await Task.Delay(2000);
            IsNotificationVisible = false;
        }
    }
}
