using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.Core.Abstractions.TibiaData;
using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Core.Services;
using TibiaHuntMaster.Infrastructure.Services.Analysis;

using static TibiaHuntMaster.App.Services.Navigation.NavigationParameters;

namespace TibiaHuntMaster.App.ViewModels.Dashboard
{
    public sealed record LevelTimelineChipItem(string DateText, string LevelText, bool IsCurrent);
    public sealed record LevelChartPointItem(double X, double Y, string Tooltip, bool IsCurrent);
    public sealed record LevelForecastChipItem(string DateText, string TimeOffsetText, string LevelText, bool IsTarget);
    public sealed record LevelForecastMarkerItem(double X, double Y, string Tooltip, bool IsTarget);
    public sealed record DepotTimelineChipItem(string DateText, string ValueText, bool IsCurrent);
    public sealed record DepotChartPointItem(double X, double Y, string Tooltip, bool IsCurrent);
    public sealed record DepotSaleChipItem(int SaleId, string DateText, string ValueText, bool IsLatest);

    public sealed partial class ProgressViewModel : ViewModelBase, INavigationAware
    {
        private const double ChartWidth = 680;
        private const double ChartHeight = 180;
        private const double ForecastChartHeightValue = 140;
        private const double DepotChartHeightValue = 150;
        private const double MarkerSize = 12;
        private const double HorizontalPadding = 24;
        private const double VerticalPadding = 18;

        private readonly ICharacterService _characterService;
        private readonly IProgressInsightsService _progressInsightsService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<ProgressViewModel> _logger;

        private ProgressInsightsResult? _lastInsights;

        [ObservableProperty] private Character? _character;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private ObservableCollection<string> _progressPeriodOptions = [];
        [ObservableProperty] private int _selectedProgressPeriodIndex = 1;
        [ObservableProperty] private int _insightSessionCount;
        [ObservableProperty] private long _insightAverageXpPerHour;
        [ObservableProperty] private long _insightAverageBalancePerHunt;
        [ObservableProperty] private string _insightXpTrendText = string.Empty;
        [ObservableProperty] private string _insightBalanceTrendText = string.Empty;
        [ObservableProperty] private string _insightLevelDeltaText = string.Empty;
        [ObservableProperty] private string _insightLevelRangeText = string.Empty;
        [ObservableProperty] private string _skillTimelineStatusText = string.Empty;
        [ObservableProperty] private string _levelChartPathData = string.Empty;
        [ObservableProperty] private string _levelChartMinLabel = string.Empty;
        [ObservableProperty] private string _levelChartMaxLabel = string.Empty;
        [ObservableProperty] private int? _levelForecastTargetLevelInput;
        [ObservableProperty] private string _levelForecastStatusText = string.Empty;
        [ObservableProperty] private string _levelForecastSummaryText = string.Empty;
        [ObservableProperty] private string _levelForecastCadenceText = string.Empty;
        [ObservableProperty] private string _levelForecastPathData = string.Empty;
        [ObservableProperty] private string _levelForecastMinLabel = string.Empty;
        [ObservableProperty] private string _levelForecastMaxLabel = string.Empty;
        [ObservableProperty] private string _levelForecastStartLabel = string.Empty;
        [ObservableProperty] private string _levelForecastEndLabel = string.Empty;
        [ObservableProperty] private double _levelForecastStartPointX;
        [ObservableProperty] private double _levelForecastStartPointY;
        [ObservableProperty] private ObservableCollection<LevelForecastChipItem> _levelForecastItems = [];
        [ObservableProperty] private ObservableCollection<LevelForecastMarkerItem> _levelForecastMarkers = [];
        [ObservableProperty] private long _depotOpenValue;
        [ObservableProperty] private string _depotSummaryText = string.Empty;
        [ObservableProperty] private long _depotRealizedValueInPeriod;
        [ObservableProperty] private string _depotRealizedSummaryText = string.Empty;
        [ObservableProperty] private string _depotLastSaleText = string.Empty;
        [ObservableProperty] private string _depotSellRhythmText = string.Empty;
        [ObservableProperty] private string _depotActionStatusText = string.Empty;
        [ObservableProperty] private string _depotDialogValidationError = string.Empty;
        [ObservableProperty] private bool _isDepotSaleDialogOpen;
        [ObservableProperty] private string _manualDepotSaleAmountInput = string.Empty;
        [ObservableProperty] private DateTimeOffset _manualDepotSaleDate = DateTimeOffset.Now;
        [ObservableProperty] private ObservableCollection<int> _manualDepotSaleHours = [];
        [ObservableProperty] private ObservableCollection<int> _manualDepotSaleMinutes = [];
        [ObservableProperty] private int _selectedManualDepotSaleHour = DateTimeOffset.Now.Hour;
        [ObservableProperty] private int _selectedManualDepotSaleMinute = DateTimeOffset.Now.Minute;
        [ObservableProperty] private string _depotChartPathData = string.Empty;
        [ObservableProperty] private string _depotChartMinLabel = string.Empty;
        [ObservableProperty] private string _depotChartMaxLabel = string.Empty;
        [ObservableProperty] private ObservableCollection<LevelTimelineChipItem> _levelTimelineItems = [];
        [ObservableProperty] private ObservableCollection<LevelChartPointItem> _levelChartPoints = [];
        [ObservableProperty] private ObservableCollection<DepotTimelineChipItem> _depotTimelineItems = [];
        [ObservableProperty] private ObservableCollection<DepotChartPointItem> _depotChartPoints = [];
        [ObservableProperty] private ObservableCollection<DepotSaleChipItem> _recentDepotSales = [];

        public ProgressViewModel(
            ICharacterService characterService,
            IProgressInsightsService progressInsightsService,
            ILocalizationService localizationService,
            ILogger<ProgressViewModel> logger)
        {
            _characterService = characterService;
            _progressInsightsService = progressInsightsService;
            _localizationService = localizationService;
            _logger = logger;

            InitializeDepotSalePickerOptions();
            UpdateProgressPeriodOptions();
            ApplyInsights(ProgressInsightsResult.Empty(InsightPeriod.Last30Days, 0));
            _localizationService.PropertyChanged += OnLanguageChanged;
        }

        public ProgressViewModel()
        {
            _characterService = null!;
            _progressInsightsService = null!;
            _localizationService = null!;
            _logger = null!;
            _progressPeriodOptions =
            [
                "7d",
                "30d",
                "90d",
                "All"
            ];
            InitializeDepotSalePickerOptions();
            _skillTimelineStatusText = "No skill data yet";
            _levelForecastStatusText = "No forecast yet";
        }

        public double LevelChartWidth => ChartWidth;

        public double LevelChartHeight => ChartHeight;

        public double LevelForecastChartHeight => ForecastChartHeightValue;

        public double DepotChartWidth => ChartWidth;

        public double DepotChartHeight => DepotChartHeightValue;

        public bool HasLevelTimeline => LevelChartPoints.Count > 0;

        public bool HasLevelForecast => LevelForecastMarkers.Count > 0;

        public bool HasDepotTimeline => DepotChartPoints.Count > 0;

        public bool HasRecentDepotSales => RecentDepotSales.Count > 0;

        public bool HasSkillData => _lastInsights?.HasSkillData == true;

        private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateProgressPeriodOptions();
            if (_lastInsights != null)
            {
                ApplyInsights(_lastInsights);
            }

            OnPropertyChanged(nameof(HasSkillData));
        }

        public void OnNavigatedTo(object? parameter)
        {
            if (parameter is ProgressWithCharacter progressParam)
            {
                RunSafeFireAndForget(LoadDataAsync(progressParam.Character), nameof(LoadDataAsync));
            }
        }

        public void OnNavigatedFrom()
        {
        }

        partial void OnSelectedProgressPeriodIndexChanged(int value)
        {
            if (Character != null)
            {
                RunSafeFireAndForget(LoadInsightsAsync(), nameof(LoadInsightsAsync));
            }
        }

        partial void OnLevelForecastTargetLevelInputChanged(int? value)
        {
            if (!HasLevelForecast)
            {
                LevelForecastStatusText = _localizationService["Progress_LevelForecastNoData"];
            }
        }

        partial void OnManualDepotSaleAmountInputChanged(string value)
        {
            DepotDialogValidationError = string.Empty;
        }

        partial void OnManualDepotSaleDateChanged(DateTimeOffset value)
        {
            DepotDialogValidationError = string.Empty;
        }

        partial void OnSelectedManualDepotSaleHourChanged(int value)
        {
            DepotDialogValidationError = string.Empty;
        }

        partial void OnSelectedManualDepotSaleMinuteChanged(int value)
        {
            DepotDialogValidationError = string.Empty;
        }

        private void InitializeDepotSalePickerOptions()
        {
            ManualDepotSaleHours.Clear();
            ManualDepotSaleMinutes.Clear();

            foreach (int hour in Enumerable.Range(0, 24))
            {
                ManualDepotSaleHours.Add(hour);
            }

            foreach (int minute in Enumerable.Range(0, 60))
            {
                ManualDepotSaleMinutes.Add(minute);
            }
        }

        private void ResetManualDepotSaleDraft(DateTimeOffset timestamp, long amount)
        {
            DateTimeOffset localTimestamp = timestamp.ToLocalTime();
            ManualDepotSaleDate = localTimestamp;
            SelectedManualDepotSaleHour = localTimestamp.Hour;
            SelectedManualDepotSaleMinute = localTimestamp.Minute;
            ManualDepotSaleAmountInput = amount > 0
                ? amount.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
            DepotDialogValidationError = string.Empty;
        }

        private void UpdateProgressPeriodOptions()
        {
            InsightPeriod currentPeriod = GetSelectedInsightPeriod();

            ProgressPeriodOptions.Clear();
            ProgressPeriodOptions.Add(_localizationService["Overview_Period7Days"]);
            ProgressPeriodOptions.Add(_localizationService["Overview_Period30Days"]);
            ProgressPeriodOptions.Add(_localizationService["Overview_Period90Days"]);
            ProgressPeriodOptions.Add(_localizationService["Overview_PeriodAll"]);

            SelectedProgressPeriodIndex = currentPeriod switch
            {
                InsightPeriod.Last7Days => 0,
                InsightPeriod.Last30Days => 1,
                InsightPeriod.Last90Days => 2,
                InsightPeriod.AllTime => 3,
                _ => 1
            };
        }

        private async Task LoadDataAsync(Character lightweightCharacter)
        {
            if (lightweightCharacter == null)
            {
                return;
            }

            IsLoading = true;
            try
            {
                Character = await _characterService.GetByNameAsync(lightweightCharacter.Name) ?? lightweightCharacter;
                await LoadInsightsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load progress data for character '{CharacterName}'", lightweightCharacter.Name);
                Character = lightweightCharacter;
                ApplyInsights(ProgressInsightsResult.Empty(GetSelectedInsightPeriod(), lightweightCharacter.Level));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadInsightsAsync()
        {
            if (Character == null)
            {
                return;
            }

            ProgressInsightsResult insights = await _progressInsightsService.GetInsightsAsync(Character.Id, Character.Level, GetSelectedInsightPeriod());
            ApplyInsights(insights);
        }

        private InsightPeriod GetSelectedInsightPeriod()
        {
            return SelectedProgressPeriodIndex switch
            {
                0 => InsightPeriod.Last7Days,
                1 => InsightPeriod.Last30Days,
                2 => InsightPeriod.Last90Days,
                3 => InsightPeriod.AllTime,
                _ => InsightPeriod.Last30Days
            };
        }

        private void ApplyInsights(ProgressInsightsResult insights)
        {
            _lastInsights = insights;
            InsightSessionCount = insights.SessionCount;
            InsightAverageXpPerHour = insights.XpPerHour.CurrentValue;
            InsightAverageBalancePerHunt = insights.BalancePerHunt.CurrentValue;
            InsightXpTrendText = FormatTrendText(insights.XpPerHour);
            InsightBalanceTrendText = FormatTrendText(insights.BalancePerHunt);
            InsightLevelDeltaText = FormatLevelDeltaText(insights.LevelProgress);
            InsightLevelRangeText = FormatLevelRangeText(insights.LevelProgress);
            DepotOpenValue = insights.DepotProgress.CurrentOpenValue;
            DepotSummaryText = insights.DepotProgress.ContributingSessions > 0
                ? string.Format(_localizationService["Progress_DepotValueSummary"], insights.DepotProgress.ContributingSessions)
                : _localizationService["Progress_NoDepotTimeline"];
            DepotRealizedValueInPeriod = insights.DepotProgress.RealizedValueInPeriod;
            DepotRealizedSummaryText = insights.DepotProgress.SaleCountInPeriod > 0
                ? string.Format(_localizationService["Progress_DepotSalesSummary"], insights.DepotProgress.SaleCountInPeriod)
                : _localizationService["Progress_NoDepotSalesYet"];
            DepotLastSaleText = insights.DepotProgress.LastSaleAtUtc.HasValue
                ? insights.DepotProgress.LastSaleAtUtc.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm", _localizationService.CurrentCulture)
                : _localizationService["Progress_NoDepotSalesYet"];
            DepotSellRhythmText = insights.DepotProgress.AverageDaysBetweenSales.HasValue
                ? string.Format(_localizationService["Progress_SellRhythmAverage"], insights.DepotProgress.AverageDaysBetweenSales.Value)
                : insights.DepotProgress.SaleCountInPeriod == 1
                    ? _localizationService["Progress_SellRhythmSingleSale"]
                    : _localizationService["Progress_SellRhythmNoData"];
            SkillTimelineStatusText = insights.HasSkillData
                ? _localizationService["Overview_SkillDataAvailable"]
                : _localizationService["Progress_NoSkillHistoryYet"];
            RebuildLevelChart(insights.LevelTimeline);
            ApplyDefaultLevelForecastTarget();
            if (HasLevelForecast)
            {
                CalculateLevelForecast();
            }
            else
            {
                ResetLevelForecast();
            }
            RebuildDepotChart(insights.DepotProgress.Timeline);
            RebuildRecentDepotSales(insights.DepotProgress.RecentSales);
            OnPropertyChanged(nameof(HasLevelTimeline));
            OnPropertyChanged(nameof(HasLevelForecast));
            OnPropertyChanged(nameof(HasDepotTimeline));
            OnPropertyChanged(nameof(HasRecentDepotSales));
            OnPropertyChanged(nameof(HasSkillData));
        }

        private string FormatTrendText(TrendInsight trend)
        {
            return trend.Direction switch
            {
                InsightTrendDirection.Up => string.Format(_localizationService["Overview_TrendUp"], Math.Abs(trend.ChangePercent)),
                InsightTrendDirection.Down => string.Format(_localizationService["Overview_TrendDown"], Math.Abs(trend.ChangePercent)),
                InsightTrendDirection.Flat => _localizationService["Overview_TrendFlat"],
                _ => _localizationService["Overview_TrendNoData"]
            };
        }

        private static string FormatLevelDeltaText(LevelProgressInsight insight)
        {
            return insight.Delta > 0 ? $"+{insight.Delta}" : insight.Delta.ToString(CultureInfo.InvariantCulture);
        }

        private string FormatLevelRangeText(LevelProgressInsight insight)
        {
            if (!insight.HasBaseline)
            {
                return _localizationService["Overview_NoSnapshotBaseline"];
            }

            return $"{insight.StartLevel} -> {insight.CurrentLevel}";
        }

        private void RebuildLevelChart(IReadOnlyList<LevelTimelinePoint> timeline)
        {
            LevelTimelineItems.Clear();
            LevelChartPoints.Clear();
            LevelChartPathData = string.Empty;
            LevelChartMinLabel = string.Empty;
            LevelChartMaxLabel = string.Empty;

            if (timeline.Count == 0)
            {
                return;
            }

            int minLevel = timeline.Min(point => point.Level);
            int maxLevel = timeline.Max(point => point.Level);
            int levelRange = Math.Max(1, maxLevel - minLevel);

            LevelChartMinLabel = $"Lv {minLevel}";
            LevelChartMaxLabel = $"Lv {maxLevel}";

            double plotWidth = ChartWidth - HorizontalPadding * 2;
            double plotHeight = ChartHeight - VerticalPadding * 2;

            List<(double CenterX, double CenterY)> centers = new();
            for (int i = 0; i < timeline.Count; i++)
            {
                LevelTimelinePoint point = timeline[i];
                double progress = timeline.Count == 1 ? 0.5 : i / (double)(timeline.Count - 1);
                double centerX = HorizontalPadding + progress * plotWidth;
                double normalizedLevel = levelRange == 0 ? 0.5 : (point.Level - minLevel) / (double)levelRange;
                double centerY = VerticalPadding + (1d - normalizedLevel) * plotHeight;
                centers.Add((centerX, centerY));

                bool isCurrent = i == timeline.Count - 1;
                string dateText = point.Timestamp.ToLocalTime().ToString("dd MMM", _localizationService.CurrentCulture);
                string levelText = $"Lv {point.Level}";
                string tooltip = $"{dateText}: {levelText}";

                LevelChartPoints.Add(new LevelChartPointItem(
                    centerX - MarkerSize / 2,
                    centerY - MarkerSize / 2,
                    tooltip,
                    isCurrent));

                LevelTimelineItems.Add(new LevelTimelineChipItem(dateText, levelText, isCurrent));
            }

            LevelChartPathData = BuildLinePath(centers);
        }

        private void RebuildDepotChart(IReadOnlyList<DepotTimelinePoint> timeline)
        {
            DepotTimelineItems.Clear();
            DepotChartPoints.Clear();
            DepotChartPathData = string.Empty;
            DepotChartMinLabel = string.Empty;
            DepotChartMaxLabel = string.Empty;

            if (timeline.Count == 0)
            {
                return;
            }

            long minValue = timeline.Min(point => point.Value);
            long maxValue = timeline.Max(point => point.Value);
            long valueRange = Math.Max(1, maxValue - minValue);

            DepotChartMinLabel = FormatCurrencyLabel(minValue);
            DepotChartMaxLabel = FormatCurrencyLabel(maxValue);

            double plotWidth = ChartWidth - HorizontalPadding * 2;
            double plotHeight = DepotChartHeightValue - VerticalPadding * 2;

            List<(double CenterX, double CenterY)> centers = new();
            for (int i = 0; i < timeline.Count; i++)
            {
                DepotTimelinePoint point = timeline[i];
                double progress = timeline.Count == 1 ? 0.5 : i / (double)(timeline.Count - 1);
                double centerX = HorizontalPadding + progress * plotWidth;
                double normalizedValue = valueRange == 0 ? 0.5 : (point.Value - minValue) / (double)valueRange;
                double centerY = VerticalPadding + (1d - normalizedValue) * plotHeight;
                centers.Add((centerX, centerY));

                bool isCurrent = i == timeline.Count - 1;
                string dateText = point.Timestamp.ToLocalTime().ToString("dd MMM", _localizationService.CurrentCulture);
                string valueText = FormatCurrencyLabel(point.Value);
                string tooltip = $"{dateText}: {valueText}";

                DepotChartPoints.Add(new DepotChartPointItem(
                    centerX - MarkerSize / 2,
                    centerY - MarkerSize / 2,
                    tooltip,
                    isCurrent));

                DepotTimelineItems.Add(new DepotTimelineChipItem(dateText, valueText, isCurrent));
            }

            DepotChartPathData = BuildLinePath(centers);
        }

        private void RebuildRecentDepotSales(IReadOnlyList<DepotSalePoint> sales)
        {
            RecentDepotSales.Clear();

            for (int i = 0; i < sales.Count; i++)
            {
                DepotSalePoint sale = sales[i];
                bool isLatest = i == sales.Count - 1;
                string dateText = sale.Timestamp.ToLocalTime().ToString("dd MMM", _localizationService.CurrentCulture);
                string valueText = FormatCurrencyLabel(sale.RealizedValue);
                RecentDepotSales.Add(new DepotSaleChipItem(sale.Id, dateText, valueText, isLatest));
            }
        }

        private static string BuildLinePath(IReadOnlyList<(double CenterX, double CenterY)> centers)
        {
            if (centers.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" ", centers.Select((point, index) =>
                string.Format(
                    CultureInfo.InvariantCulture,
                    index == 0 ? "M {0:0.##},{1:0.##}" : "L {0:0.##},{1:0.##}",
                    point.CenterX,
                    point.CenterY)));
        }

        private string FormatCurrencyLabel(long value)
        {
            return $"{value.ToString("N0", _localizationService.CurrentCulture)} gp";
        }

        private void ApplyDefaultLevelForecastTarget()
        {
            if (Character == null)
            {
                return;
            }

            if (!LevelForecastTargetLevelInput.HasValue || LevelForecastTargetLevelInput.Value <= Character.Level)
            {
                LevelForecastTargetLevelInput = Character.Level + 1;
            }

            if (string.IsNullOrWhiteSpace(LevelForecastStatusText))
            {
                LevelForecastStatusText = _localizationService["Progress_LevelForecastNoData"];
            }
        }

        private void ResetLevelForecast()
        {
            LevelForecastItems.Clear();
            LevelForecastMarkers.Clear();
            LevelForecastSummaryText = string.Empty;
            LevelForecastCadenceText = string.Empty;
            LevelForecastPathData = string.Empty;
            LevelForecastMinLabel = string.Empty;
            LevelForecastMaxLabel = string.Empty;
            LevelForecastStartLabel = string.Empty;
            LevelForecastEndLabel = string.Empty;
            LevelForecastStartPointX = 0d;
            LevelForecastStartPointY = 0d;
            LevelForecastStatusText = _localizationService["Progress_LevelForecastNoData"];
            OnPropertyChanged(nameof(HasLevelForecast));
        }

        [RelayCommand]
        private void CalculateLevelForecast()
        {
            LevelForecastItems.Clear();
            LevelForecastMarkers.Clear();
            LevelForecastSummaryText = string.Empty;
            LevelForecastCadenceText = string.Empty;
            LevelForecastPathData = string.Empty;
            LevelForecastMinLabel = string.Empty;
            LevelForecastMaxLabel = string.Empty;
            LevelForecastStartLabel = string.Empty;
            LevelForecastEndLabel = string.Empty;
            LevelForecastStartPointX = 0d;
            LevelForecastStartPointY = 0d;

            if (Character == null || _lastInsights == null)
            {
                LevelForecastStatusText = _localizationService["Progress_LevelForecastNoData"];
                OnPropertyChanged(nameof(HasLevelForecast));
                return;
            }

            int currentLevel = Character.Level;
            int targetLevel = LevelForecastTargetLevelInput ?? 0;
            if (targetLevel <= currentLevel)
            {
                LevelForecastStatusText = string.Format(
                    _localizationService["Progress_LevelForecastNeedHigherLevel"],
                    currentLevel + 1);
                OnPropertyChanged(nameof(HasLevelForecast));
                return;
            }

            long xpPerHour = _lastInsights.XpPerHour.CurrentValue;
            if (xpPerHour <= 0)
            {
                LevelForecastStatusText = _localizationService["Progress_LevelForecastNoXpData"];
                OnPropertyChanged(nameof(HasLevelForecast));
                return;
            }

            double huntHoursPerDay = _lastInsights.HuntCadence.AverageHuntHoursPerDay;
            if (huntHoursPerDay <= 0)
            {
                LevelForecastStatusText = _localizationService["Progress_LevelForecastNoCadenceData"];
                OnPropertyChanged(nameof(HasLevelForecast));
                return;
            }

            DateTimeOffset now = DateTimeOffset.Now;
            long estimatedCurrentXp = EstimateCurrentExperience(currentLevel, xpPerHour, _lastInsights.LevelTimeline, now, out bool usedEstimatedCurrentProgress);
            long targetXp = TibiaMathService.ExperienceForLevel(targetLevel);
            long totalRemainingXp = Math.Max(0, targetXp - estimatedCurrentXp);
            if (totalRemainingXp <= 0)
            {
                LevelForecastStatusText = _localizationService["Progress_LevelForecastNoData"];
                OnPropertyChanged(nameof(HasLevelForecast));
                return;
            }

            double totalHuntHours = totalRemainingXp / (double)xpPerHour;
            double totalCalendarDays = totalHuntHours / huntHoursPerDay;
            DateTimeOffset finalTimestamp = now.AddDays(totalCalendarDays);
            LevelForecastStartLabel = _localizationService["Progress_LevelForecastNow"];
            LevelForecastEndLabel = finalTimestamp.ToLocalTime().ToString("dd MMM HH:mm", _localizationService.CurrentCulture);
            LevelForecastMinLabel = $"Lv {currentLevel}";
            LevelForecastMaxLabel = $"Lv {targetLevel}";
            LevelForecastCadenceText = string.Format(
                _localizationService["Progress_LevelForecastCadence"],
                huntHoursPerDay.ToString("N1", _localizationService.CurrentCulture),
                _lastInsights.HuntCadence.ObservedDays,
                _lastInsights.HuntCadence.SessionCount);

            double plotWidth = ChartWidth - HorizontalPadding * 2;
            double plotHeight = ForecastChartHeightValue - VerticalPadding * 2;
            long currentLevelBaseXp = TibiaMathService.ExperienceForLevel(currentLevel);
            long nextLevelXp = TibiaMathService.ExperienceForLevel(currentLevel + 1);
            double currentLevelProgress = nextLevelXp <= currentLevelBaseXp
                ? 0d
                : Math.Clamp((estimatedCurrentXp - currentLevelBaseXp) / (double)(nextLevelXp - currentLevelBaseXp), 0d, 0.999d);
            double startLevelValue = currentLevel + currentLevelProgress;
            double levelRange = Math.Max(1d, targetLevel - startLevelValue);
            double startCenterX = HorizontalPadding;
            double startCenterY = VerticalPadding + plotHeight;
            LevelForecastStartPointX = startCenterX - MarkerSize / 2;
            LevelForecastStartPointY = startCenterY - MarkerSize / 2;

            List<(double CenterX, double CenterY)> lineCenters = [ (startCenterX, startCenterY) ];

            for (int level = currentLevel + 1; level <= targetLevel; level++)
            {
                long levelXp = TibiaMathService.ExperienceForLevel(level);
                long remainingXp = Math.Max(0, levelXp - estimatedCurrentXp);
                double huntHoursUntilLevel = remainingXp / (double)xpPerHour;
                double calendarDaysUntilLevel = huntHoursUntilLevel / huntHoursPerDay;
                DateTimeOffset predictedTimestamp = now.AddDays(calendarDaysUntilLevel);
                double progress = totalCalendarDays <= 0.01d ? 0.5d : Math.Clamp(calendarDaysUntilLevel / totalCalendarDays, 0d, 1d);
                double centerX = HorizontalPadding + progress * plotWidth;
                double normalizedLevel = Math.Clamp((level - startLevelValue) / levelRange, 0d, 1d);
                double centerY = VerticalPadding + (1d - normalizedLevel) * plotHeight;
                string dateText = predictedTimestamp.ToLocalTime().ToString("dd MMM HH:mm", _localizationService.CurrentCulture);
                string offsetText = FormatDurationCompact(TimeSpan.FromDays(calendarDaysUntilLevel));
                string levelText = $"Lv {level}";
                string tooltip = string.Format(_localizationService["Progress_LevelForecastMarkerTooltip"], levelText, dateText, offsetText);

                lineCenters.Add((centerX, centerY));
                LevelForecastMarkers.Add(new LevelForecastMarkerItem(
                    centerX - MarkerSize / 2,
                    centerY - MarkerSize / 2,
                    tooltip,
                    level == targetLevel));

                LevelForecastItems.Add(new LevelForecastChipItem(dateText, offsetText, levelText, level == targetLevel));
            }

            LevelForecastPathData = BuildLinePath(lineCenters);

            LevelForecastSummaryText = string.Format(
                _localizationService[usedEstimatedCurrentProgress
                    ? "Progress_LevelForecastSummaryEstimated"
                    : "Progress_LevelForecastSummaryLevelStart"],
                FormatDurationCompact(TimeSpan.FromDays(totalCalendarDays)),
                xpPerHour.ToString("N0", _localizationService.CurrentCulture),
                $"Lv {targetLevel}",
                huntHoursPerDay.ToString("N1", _localizationService.CurrentCulture));
            LevelForecastStatusText = string.Empty;
            OnPropertyChanged(nameof(HasLevelForecast));
        }

        private static long EstimateCurrentExperience(
            int currentLevel,
            long xpPerHour,
            IReadOnlyList<LevelTimelinePoint> timeline,
            DateTimeOffset now,
            out bool usedEstimatedCurrentProgress)
        {
            long levelBaseXp = TibiaMathService.ExperienceForLevel(currentLevel);
            long nextLevelXp = TibiaMathService.ExperienceForLevel(currentLevel + 1);
            long maxProgressInLevel = Math.Max(0, nextLevelXp - levelBaseXp - 1);

            LevelTimelinePoint? currentLevelEntry = timeline.FirstOrDefault(point => point.Level == currentLevel);
            if (currentLevelEntry == null)
            {
                usedEstimatedCurrentProgress = false;
                return levelBaseXp;
            }

            double elapsedHours = Math.Max(0d, (now - currentLevelEntry.Timestamp.ToLocalTime()).TotalHours);
            long estimatedProgress = (long)Math.Round(xpPerHour * elapsedHours, MidpointRounding.AwayFromZero);
            estimatedProgress = Math.Clamp(estimatedProgress, 0, maxProgressInLevel);
            usedEstimatedCurrentProgress = estimatedProgress > 0;
            return levelBaseXp + estimatedProgress;
        }

        private string FormatDurationCompact(TimeSpan duration)
        {
            if (duration.TotalHours < 1)
            {
                return string.Format(
                    _localizationService["Progress_LevelForecastMinutesOnly"],
                    Math.Max(1, (int)Math.Round(duration.TotalMinutes, MidpointRounding.AwayFromZero)));
            }

            int days = (int)duration.TotalDays;
            int hours = duration.Hours;
            int minutes = duration.Minutes;

            if (days > 0)
            {
                return minutes > 0
                    ? string.Format(_localizationService["Progress_LevelForecastDaysHoursMinutes"], days, hours, minutes)
                    : string.Format(_localizationService["Progress_LevelForecastDaysHours"], days, hours);
            }

            return minutes > 0
                ? string.Format(_localizationService["Progress_LevelForecastHoursMinutes"], (int)duration.TotalHours, minutes)
                : string.Format(_localizationService["Progress_LevelForecastHoursOnly"], (int)Math.Round(duration.TotalHours, MidpointRounding.AwayFromZero));
        }

        [RelayCommand]
        private void OpenDepotSaleDialog()
        {
            ResetManualDepotSaleDraft(DateTimeOffset.Now, DepotOpenValue);
            IsDepotSaleDialogOpen = true;
        }

        [RelayCommand]
        private void CloseDepotSaleDialog()
        {
            DepotDialogValidationError = string.Empty;
            IsDepotSaleDialogOpen = false;
        }

        [RelayCommand]
        private async Task MarkDepotSoldNowAsync()
        {
            if (Character == null)
            {
                return;
            }

            if (DepotOpenValue <= 0)
            {
                DepotActionStatusText = _localizationService["Progress_DepotNothingToSell"];
                return;
            }

            IsLoading = true;
            try
            {
                await _progressInsightsService.RecordDepotSaleAsync(Character.Id, DepotOpenValue, DateTimeOffset.UtcNow);
                DepotActionStatusText = string.Format(_localizationService["Progress_DepotSaleSaved"], FormatCurrencyLabel(DepotOpenValue));
                await LoadInsightsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark depot sold now for character '{CharacterName}'", Character.Name);
                DepotActionStatusText = _localizationService["Progress_DepotSaleSaveFailed"];
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveDepotSaleAsync()
        {
            if (Character == null)
            {
                return;
            }

            if (!TryBuildManualDepotSale(out long amount, out DateTimeOffset soldAt))
            {
                return;
            }

            IsLoading = true;
            try
            {
                await _progressInsightsService.RecordDepotSaleAsync(Character.Id, amount, soldAt);
                DepotActionStatusText = string.Format(_localizationService["Progress_DepotSaleSaved"], FormatCurrencyLabel(amount));
                DepotDialogValidationError = string.Empty;
                IsDepotSaleDialogOpen = false;
                await LoadInsightsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save manual depot sale for character '{CharacterName}'", Character.Name);
                DepotDialogValidationError = _localizationService["Progress_DepotSaleSaveFailed"];
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool TryBuildManualDepotSale(out long amount, out DateTimeOffset soldAt)
        {
            amount = 0;
            soldAt = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(ManualDepotSaleAmountInput))
            {
                DepotDialogValidationError = _localizationService["Progress_ManualSaleAmountRequired"];
                return false;
            }

            if (!long.TryParse(ManualDepotSaleAmountInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out amount) || amount <= 0)
            {
                DepotDialogValidationError = _localizationService["Progress_ManualSaleInvalidAmount"];
                return false;
            }

            DateTime localDateTime = new(
                ManualDepotSaleDate.Year,
                ManualDepotSaleDate.Month,
                ManualDepotSaleDate.Day,
                SelectedManualDepotSaleHour,
                SelectedManualDepotSaleMinute,
                0,
                DateTimeKind.Local);

            soldAt = new DateTimeOffset(localDateTime);
            if (soldAt > DateTimeOffset.Now.AddMinutes(1))
            {
                DepotDialogValidationError = _localizationService["Progress_ManualSaleFutureDate"];
                return false;
            }

            return true;
        }

        private static void RunSafeFireAndForget(Task task, string operationName)
        {
            _ = ObserveTaskAsync(task, operationName);
        }

        private static async Task ObserveTaskAsync(Task task, string operationName)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{nameof(ProgressViewModel)}] {operationName} failed: {ex}");
            }
        }
    }
}
