using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.ViewModels.Dashboard;
using TibiaHuntMaster.Core.Abstractions.TibiaData;
using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Infrastructure.Services.Analysis;

namespace TibiaHuntMaster.Tests.ViewModels
{
    public sealed class ProgressViewModelTests
    {
        private readonly Mock<ICharacterService> _characterServiceMock;
        private readonly Mock<IProgressInsightsService> _progressInsightsServiceMock;
        private readonly Mock<ILocalizationService> _localizationServiceMock;

        public ProgressViewModelTests()
        {
            _characterServiceMock = new Mock<ICharacterService>();
            _progressInsightsServiceMock = new Mock<IProgressInsightsService>();
            _localizationServiceMock = new Mock<ILocalizationService>();

            Dictionary<string, string> translations = new(StringComparer.Ordinal)
            {
                ["Overview_Period7Days"] = "Last 7 days",
                ["Overview_Period30Days"] = "Last 30 days",
                ["Overview_Period90Days"] = "Last 90 days",
                ["Overview_PeriodAll"] = "All time",
                ["Overview_TrendUp"] = "UP {0:N1}",
                ["Overview_TrendDown"] = "DOWN {0:N1}",
                ["Overview_TrendFlat"] = "FLAT",
                ["Overview_TrendNoData"] = "NO DATA",
                ["Overview_NoSnapshotBaseline"] = "NO SNAPSHOT BASELINE",
                ["Overview_SkillDataAvailable"] = "SKILL SNAPSHOTS AVAILABLE",
                ["Progress_DepotValueSummary"] = "Across {0} hunts since the last sale",
                ["Progress_NoDepotTimeline"] = "NO DEPOT DATA YET",
                ["Progress_DepotSalesSummary"] = "{0} sales in this period",
                ["Progress_NoDepotSalesYet"] = "NO SALES YET",
                ["Progress_SellRhythmAverage"] = "Average every {0} days",
                ["Progress_SellRhythmSingleSale"] = "Only one sale in this period",
                ["Progress_SellRhythmNoData"] = "NO RHYTHM DATA",
                ["Progress_NoSkillHistoryYet"] = "NO SKILL DATA YET",
                ["Progress_LevelForecastNoData"] = "NO FORECAST YET",
                ["Progress_LevelForecastNeedHigherLevel"] = "NEEDS LV {0}",
                ["Progress_LevelForecastNoXpData"] = "NO XP DATA",
                ["Progress_LevelForecastNoCadenceData"] = "NO CADENCE DATA",
                ["Progress_LevelForecastSummaryEstimated"] = "ESTIMATED {0} TO {2} AT {1} XP/H",
                ["Progress_LevelForecastSummaryLevelStart"] = "FROM LEVEL START {0} TO {2} AT {1} XP/H",
                ["Progress_LevelForecastCadence"] = "BASED ON {0} H/DAY FROM {2} HUNTS ACROSS {1} DAYS",
                ["Progress_LevelForecastMarkerTooltip"] = "{0} | {1} | {2}",
                ["Progress_LevelForecastNow"] = "NOW",
                ["Progress_LevelForecastHoursOnly"] = "{0}h",
                ["Progress_LevelForecastHoursMinutes"] = "{0}h {1}m",
                ["Progress_LevelForecastDaysHours"] = "{0}d {1}h",
                ["Progress_LevelForecastDaysHoursMinutes"] = "{0}d {1}h {2}m",
                ["Progress_LevelForecastMinutesOnly"] = "{0}m"
            };

            _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => translations.TryGetValue(key, out string? value) ? value : key);

            _localizationServiceMock
            .SetupGet(x => x.CurrentCulture)
            .Returns(System.Globalization.CultureInfo.InvariantCulture);
        }

        [Fact]
        public void OnNavigatedTo_ShouldBuildLevelChartAndSkillFallback_WhenInsightsHaveNoSkillData()
        {
            Character lightweight = new() { Id = 7, Name = "Tentakel", Level = 620 };
            Character loaded = new() { Id = 7, Name = "Tentakel", Level = 620 };

            _characterServiceMock.Setup(x => x.GetByNameAsync("Tentakel", It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(loaded);

            ProgressInsightsResult insights = new(
                InsightPeriod.Last30Days,
                4,
                new TrendInsight(5_200_000, 4_900_000, InsightTrendDirection.Up, 6.1d, 4, 4),
                new TrendInsight(310_000, 280_000, InsightTrendDirection.Up, 10.7d, 4, 4),
                new LevelProgressInsight(615, 620, 5, 4, true),
                new[]
                {
                    new LevelTimelinePoint(new DateTimeOffset(2026, 03, 01, 0, 0, 0, TimeSpan.Zero), 615),
                    new LevelTimelinePoint(new DateTimeOffset(2026, 03, 05, 0, 0, 0, TimeSpan.Zero), 617),
                    new LevelTimelinePoint(new DateTimeOffset(2026, 03, 10, 0, 0, 0, TimeSpan.Zero), 620)
                },
                new DepotValueInsight(
                    820_000,
                    4,
                    new[]
                    {
                        new DepotTimelinePoint(new DateTimeOffset(2026, 03, 01, 0, 0, 0, TimeSpan.Zero), 180_000),
                        new DepotTimelinePoint(new DateTimeOffset(2026, 03, 05, 0, 0, 0, TimeSpan.Zero), 430_000),
                        new DepotTimelinePoint(new DateTimeOffset(2026, 03, 10, 0, 0, 0, TimeSpan.Zero), 820_000)
                    },
                    530_000,
                    2,
                    new DateTimeOffset(2026, 03, 11, 12, 0, 0, TimeSpan.Zero),
                    5.0d,
                    new[]
                    {
                        new DepotSalePoint(1, new DateTimeOffset(2026, 03, 06, 12, 0, 0, TimeSpan.Zero), 250_000),
                        new DepotSalePoint(2, new DateTimeOffset(2026, 03, 11, 12, 0, 0, TimeSpan.Zero), 280_000)
                    }),
                new HuntCadenceInsight(1.3d, 1d, 7, 4),
                HasSkillData: false);

            _progressInsightsServiceMock.Setup(x => x.GetInsightsAsync(7, 620, InsightPeriod.Last30Days, It.IsAny<CancellationToken>()))
                                        .ReturnsAsync(insights);

            ProgressViewModel viewModel = CreateViewModel();

            viewModel.OnNavigatedTo(CreateProgressParameter(lightweight));

            SpinWait.SpinUntil(() => !viewModel.IsLoading, TimeSpan.FromSeconds(2)).Should().BeTrue();

            viewModel.HasLevelTimeline.Should().BeTrue();
            viewModel.LevelTimelineItems.Should().HaveCount(3);
            viewModel.LevelChartPoints.Should().HaveCount(3);
            viewModel.LevelChartPathData.Should().NotBeNullOrWhiteSpace();
            viewModel.HasDepotTimeline.Should().BeTrue();
            viewModel.DepotChartPoints.Should().HaveCount(3);
            viewModel.DepotOpenValue.Should().Be(820_000);
            viewModel.SkillTimelineStatusText.Should().Be("NO SKILL DATA YET");
            viewModel.HasSkillData.Should().BeFalse();
        }

        [Fact]
        public void OnNavigatedTo_ShouldReflectAvailableSkillSnapshots_WhenInsightsReportSkillData()
        {
            Character lightweight = new() { Id = 9, Name = "Falconer", Level = 700 };
            Character loaded = new() { Id = 9, Name = "Falconer", Level = 700 };

            _characterServiceMock.Setup(x => x.GetByNameAsync("Falconer", It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(loaded);

            ProgressInsightsResult insights = new(
                InsightPeriod.Last30Days,
                2,
                new TrendInsight(0, 0, InsightTrendDirection.NoData, 0d, 0, 0),
                new TrendInsight(0, 0, InsightTrendDirection.NoData, 0d, 0, 0),
                new LevelProgressInsight(700, 700, 0, 0, false),
                Array.Empty<LevelTimelinePoint>(),
                new DepotValueInsight(0, 0, Array.Empty<DepotTimelinePoint>(), 0, 0, null, null, Array.Empty<DepotSalePoint>()),
                new HuntCadenceInsight(0d, 0d, 0, 0),
                HasSkillData: true);

            _progressInsightsServiceMock.Setup(x => x.GetInsightsAsync(9, 700, InsightPeriod.Last30Days, It.IsAny<CancellationToken>()))
                                        .ReturnsAsync(insights);

            ProgressViewModel viewModel = CreateViewModel();

            viewModel.OnNavigatedTo(CreateProgressParameter(lightweight));

            SpinWait.SpinUntil(() => !viewModel.IsLoading, TimeSpan.FromSeconds(2)).Should().BeTrue();

            viewModel.HasSkillData.Should().BeTrue();
            viewModel.SkillTimelineStatusText.Should().Be("SKILL SNAPSHOTS AVAILABLE");
            viewModel.HasLevelTimeline.Should().BeFalse();
            viewModel.HasDepotTimeline.Should().BeFalse();
        }

        [Fact]
        public void CalculateLevelForecast_ShouldBuildMarkersAndDefaultTargetLevel()
        {
            Character lightweight = new() { Id = 11, Name = "Prophet", Level = 620 };
            Character loaded = new() { Id = 11, Name = "Prophet", Level = 620 };

            _characterServiceMock.Setup(x => x.GetByNameAsync("Prophet", It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(loaded);

            ProgressInsightsResult insights = new(
                InsightPeriod.Last30Days,
                5,
                new TrendInsight(5_200_000, 4_900_000, InsightTrendDirection.Up, 6.1d, 5, 4),
                new TrendInsight(0, 0, InsightTrendDirection.NoData, 0d, 0, 0),
                new LevelProgressInsight(618, 620, 2, 3, true),
                new[]
                {
                    new LevelTimelinePoint(DateTimeOffset.Now.AddDays(-8), 618),
                    new LevelTimelinePoint(DateTimeOffset.Now.AddDays(-3), 619),
                    new LevelTimelinePoint(DateTimeOffset.Now.AddHours(-2), 620)
                },
                new DepotValueInsight(0, 0, Array.Empty<DepotTimelinePoint>(), 0, 0, null, null, Array.Empty<DepotSalePoint>()),
                new HuntCadenceInsight(2d, 1d, 30, 5),
                HasSkillData: false);

            _progressInsightsServiceMock.Setup(x => x.GetInsightsAsync(11, 620, InsightPeriod.Last30Days, It.IsAny<CancellationToken>()))
                                        .ReturnsAsync(insights);

            ProgressViewModel viewModel = CreateViewModel();

            viewModel.OnNavigatedTo(CreateProgressParameter(lightweight));

            SpinWait.SpinUntil(() => !viewModel.IsLoading, TimeSpan.FromSeconds(2)).Should().BeTrue();
            viewModel.LevelForecastTargetLevelInput.Should().Be(621);
            viewModel.LevelForecastTargetLevelInput = 622;

            viewModel.CalculateLevelForecastCommand.Execute(null);

            viewModel.HasLevelForecast.Should().BeTrue();
            viewModel.LevelForecastMarkers.Should().HaveCount(2);
            viewModel.LevelForecastItems.Should().HaveCount(2);
            viewModel.LevelForecastStartLabel.Should().Be("NOW");
            viewModel.LevelForecastEndLabel.Should().NotBeNullOrWhiteSpace();
            viewModel.LevelForecastSummaryText.Should().Contain("Lv 622");
            viewModel.LevelForecastCadenceText.Should().Contain("H/DAY");
            viewModel.LevelForecastPathData.Should().NotBeNullOrWhiteSpace();
            viewModel.LevelForecastMarkers.Select(marker => marker.Y).Distinct().Should().HaveCountGreaterThan(1);
            viewModel.LevelForecastStartPointY.Should().BeGreaterThan(viewModel.LevelForecastMarkers.Min(marker => marker.Y));
        }

        private ProgressViewModel CreateViewModel()
        {
            return new ProgressViewModel(
                _characterServiceMock.Object,
                _progressInsightsServiceMock.Object,
                _localizationServiceMock.Object,
                NullLogger<ProgressViewModel>.Instance);
        }

        private static object CreateProgressParameter(Character character)
        {
            Type navigationType = typeof(ProgressViewModel).Assembly
                                                           .GetType("TibiaHuntMaster.App.Services.Navigation.NavigationParameters+ProgressWithCharacter")!;

            ConstructorInfo ctor = navigationType
                                   .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                   .Single(ctor =>
                                   {
                                       ParameterInfo[] parameters = ctor.GetParameters();
                                       return parameters.Length == 1 && parameters[0].ParameterType == typeof(Character);
                                   });
            return ctor.Invoke(new object[] { character });
        }
    }
}
