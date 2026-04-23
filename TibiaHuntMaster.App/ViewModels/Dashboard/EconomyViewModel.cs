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
using TibiaHuntMaster.Infrastructure.Services.Analysis;

using static TibiaHuntMaster.App.Services.Navigation.NavigationParameters;

namespace TibiaHuntMaster.App.ViewModels.Dashboard
{
    public sealed partial class EconomyViewModel : ViewModelBase, INavigationAware
    {
        private const double ChartWidth = 680;
        private const double ChartHeight = 180;
        private const double MarkerSize = 12;
        private const double HorizontalPadding = 24;
        private const double VerticalPadding = 18;

        private readonly ICharacterService _characterService;
        private readonly IProgressInsightsService _progressInsightsService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<EconomyViewModel> _logger;

        private ProgressInsightsResult? _lastInsights;

        [ObservableProperty] private Character? _character;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private ObservableCollection<string> _economyPeriodOptions = [];
        [ObservableProperty] private int _selectedEconomyPeriodIndex = 1;
        [ObservableProperty] private int _insightSessionCount;
        [ObservableProperty] private long _insightAverageBalancePerHunt;
        [ObservableProperty] private string _insightBalanceTrendText = string.Empty;
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
        [ObservableProperty] private ObservableCollection<DepotTimelineChipItem> _depotTimelineItems = [];
        [ObservableProperty] private ObservableCollection<DepotChartPointItem> _depotChartPoints = [];
        [ObservableProperty] private ObservableCollection<DepotSaleChipItem> _recentDepotSales = [];

        public EconomyViewModel(
            ICharacterService characterService,
            IProgressInsightsService progressInsightsService,
            ILocalizationService localizationService,
            ILogger<EconomyViewModel> logger)
        {
            _characterService = characterService;
            _progressInsightsService = progressInsightsService;
            _localizationService = localizationService;
            _logger = logger;

            InitializeDepotSalePickerOptions();
            UpdatePeriodOptions();
            ApplyInsights(ProgressInsightsResult.Empty(InsightPeriod.Last30Days, 0));
            _localizationService.PropertyChanged += OnLanguageChanged;
        }

        public EconomyViewModel()
        {
            _characterService = null!;
            _progressInsightsService = null!;
            _localizationService = null!;
            _logger = null!;
            _economyPeriodOptions =
            [
                "7d",
                "30d",
                "90d",
                "All"
            ];
            InitializeDepotSalePickerOptions();
        }

        public double DepotChartWidth => ChartWidth;

        public double DepotChartHeight => ChartHeight;

        public bool HasDepotTimeline => DepotChartPoints.Count > 0;

        public bool HasRecentDepotSales => RecentDepotSales.Count > 0;

        private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdatePeriodOptions();
            if (_lastInsights != null)
            {
                ApplyInsights(_lastInsights);
            }
        }

        public void OnNavigatedTo(object? parameter)
        {
            if (parameter is EconomyWithCharacter economyParam)
            {
                RunSafeFireAndForget(LoadDataAsync(economyParam.Character), nameof(LoadDataAsync));
            }
        }

        public void OnNavigatedFrom()
        {
        }

        partial void OnSelectedEconomyPeriodIndexChanged(int value)
        {
            if (Character != null)
            {
                RunSafeFireAndForget(LoadInsightsAsync(), nameof(LoadInsightsAsync));
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

        private void UpdatePeriodOptions()
        {
            InsightPeriod currentPeriod = GetSelectedInsightPeriod();

            EconomyPeriodOptions.Clear();
            EconomyPeriodOptions.Add(_localizationService["Overview_Period7Days"]);
            EconomyPeriodOptions.Add(_localizationService["Overview_Period30Days"]);
            EconomyPeriodOptions.Add(_localizationService["Overview_Period90Days"]);
            EconomyPeriodOptions.Add(_localizationService["Overview_PeriodAll"]);

            SelectedEconomyPeriodIndex = currentPeriod switch
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
                _logger.LogError(ex, "Failed to load economy data for character '{CharacterName}'", lightweightCharacter.Name);
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
            return SelectedEconomyPeriodIndex switch
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
            InsightAverageBalancePerHunt = insights.BalancePerHunt.CurrentValue;
            InsightBalanceTrendText = FormatTrendText(insights.BalancePerHunt);
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

            RebuildDepotChart(insights.DepotProgress.Timeline);
            RebuildRecentDepotSales(insights.DepotProgress.RecentSales);

            OnPropertyChanged(nameof(HasDepotTimeline));
            OnPropertyChanged(nameof(HasRecentDepotSales));
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
            double plotHeight = ChartHeight - VerticalPadding * 2;

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

        [RelayCommand]
        private async Task DeleteDepotSaleAsync(DepotSaleChipItem? sale)
        {
            if (Character == null || sale == null || sale.SaleId <= 0)
            {
                return;
            }

            IsLoading = true;
            try
            {
                await _progressInsightsService.DeleteDepotSaleAsync(Character.Id, sale.SaleId);
                DepotActionStatusText = _localizationService["Progress_DepotSaleDeleted"];
                await LoadInsightsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete depot sale {SaleId} for character '{CharacterName}'", sale.SaleId, Character.Name);
                DepotActionStatusText = _localizationService["Progress_DepotSaleDeleteFailed"];
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
                Debug.WriteLine($"[{nameof(EconomyViewModel)}] {operationName} failed: {ex}");
            }
        }
    }
}
