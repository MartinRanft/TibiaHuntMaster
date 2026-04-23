using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using TibiaHuntMaster.App.Services.Diagnostics;
using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Map;
using Microsoft.EntityFrameworkCore;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Services.Analysis;

namespace TibiaHuntMaster.App.Services.Summaries
{
    public sealed class HuntSummaryGeneratorService(
        AppDataPaths paths,
        ILocalizationService localizationService,
        IMonsterImageCatalogService monsterImageCatalogService,
        IDbContextFactory<AppDbContext>? dbFactory = null) : IHuntSummaryGeneratorService
    {
        private const int TargetBaselineMinutes = 15;
        private const int DefaultSoloXpRatePercent = 150;
        private const int TopItemLimit = 5;
        private const int CommonDropValueThreshold = 10_000;
        private static readonly HashSet<string> SummaryExcludedItems = new(StringComparer.OrdinalIgnoreCase)
        {
            "GOLD COIN",
            "GOLD COINS",
            "PLATINUM COIN",
            "PLATINUM COINS",
            "CRYSTAL COIN",
            "CRYSTAL COINS"
        };
        private static readonly FontFamily TitleFontFamily = FontFamily.Default;
        private static readonly FontFamily BodyFontFamily = FontFamily.Default;
        private static readonly string[] XpGainLabels = ["XP Gain", "XP Gewinn", "Przyrost XP", "Ganancia de XP", "Ganho de XP", "XP-vinst"];
        private readonly AppDataPaths _paths = paths;
        private readonly ILocalizationService _localizationService = localizationService;
        private readonly IMonsterImageCatalogService _monsterImageCatalogService = monsterImageCatalogService;
        private readonly IDbContextFactory<AppDbContext>? _dbFactory = dbFactory;
        private readonly object _itemMetadataGate = new();
        private readonly Dictionary<string, SummaryItemMetadata> _itemMetadataCache = new(StringComparer.Ordinal);

        public string BuildPreviewText(HuntSummaryRequest request)
        {
            SummaryContent content = BuildContent(request, includeMonsterIcons: false);
            return BuildPlainText(content);
        }

        public string BuildText(HuntSummaryRequest request)
        {
            SummaryContent content = BuildContent(request, includeMonsterIcons: false);

            return request.Format switch
            {
                HuntSummaryFormat.Discord => BuildDiscordText(content),
                HuntSummaryFormat.Text => BuildPlainText(content),
                HuntSummaryFormat.Image => BuildPlainText(content),
                _ => BuildPlainText(content)
            };
        }

        public async Task<string> ExportImageAsync(HuntSummaryRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _monsterImageCatalogService.EnsureCatalogAsync(cancellationToken);

            SummaryContent content = BuildContent(request, includeMonsterIcons: true);
            string filePath = Path.Combine(
                _paths.SummariesDirectory,
                $"hunt-summary-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.png");

            RenderToPng(content, filePath);
            return filePath;
        }

        private SummaryContent BuildContent(HuntSummaryRequest request, bool includeMonsterIcons)
        {
            ArgumentNullException.ThrowIfNull(request);

            if(request.SoloSession == null == (request.TeamSession == null))
            {
                throw new InvalidOperationException("Exactly one session type must be provided for a hunt summary.");
            }

            return request.TeamSession != null
                ? BuildTeamContent(request, request.TeamSession)
                : BuildSoloContent(request, request.SoloSession!, includeMonsterIcons);
        }

        private SummaryContent BuildSoloContent(HuntSummaryRequest request, HuntSessionEntity session, bool includeMonsterIcons)
        {
            TimeSpan effectiveDuration = ResolveEffectiveDuration(request.EffectiveDuration, session.Duration);
            double factor = request.NormalizeToQuarterHour ? GetQuarterHourFactor(effectiveDuration) : 1d;
            string baselineSuffix = request.NormalizeToQuarterHour ? $" ({TargetBaselineMinutes}m)" : string.Empty;
            long? rawXpGain = request.RawXpGain.HasValue && request.RawXpGain.Value > 0 ? request.RawXpGain.Value : null;
            RawXpPerHourResult rawXpPerHour = ResolveSoloRawXpPerHour(
                request.EffectiveXpPerHour,
                effectiveDuration,
                rawXpGain,
                request.AllowEstimatedRawXp,
                session.IsDoubleXp,
                request.XpBoostPercent,
                request.XpBoostActiveMinutes,
                request.CustomXpRatePercent);

            List<SummaryLine> lines =
            [
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryModeLine"], _localizationService["Analyzer_SummaryModeSolo"]), tone: SummaryTone.Muted),
                request.NormalizeToQuarterHour
                    ? SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryBaselineLine"], TargetBaselineMinutes, FormatDuration(effectiveDuration)), tone: SummaryTone.Muted)
                    : SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryDurationLine"], FormatDuration(effectiveDuration)), tone: SummaryTone.Muted),
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryXpPerHourLine"], FormatNumber(request.EffectiveXpPerHour)), tone: SummaryTone.Info),
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryXpGainLine"], FormatNumber(Scale(session.XpGain, factor))), tone: SummaryTone.Info)
            ];

            if(request.ShowRawXp && rawXpPerHour.IsAvailable && rawXpPerHour.Value > 0)
            {
                string rawXpKey = rawXpPerHour.IsEstimated
                    ? "Analyzer_SummaryRawXpPerHourEstimatedLine"
                    : "Analyzer_SummaryRawXpPerHourLine";
                lines.Add(SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService[rawXpKey], FormatNumber(rawXpPerHour.Value)), tone: SummaryTone.Info));
            }
            else if(request.ShowRawXp)
            {
                lines.Add(SummaryLine.Value(_localizationService["Analyzer_SummaryRawXpPerHourUnavailableLine"], tone: SummaryTone.Muted));
            }

            lines.AddRange(
            [
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryBalanceLine"], FormatGold(Scale(request.EffectiveBalance, factor))), tone: request.EffectiveBalance >= 0 ? SummaryTone.Success : SummaryTone.Danger),
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryLootLine"], FormatGold(Scale(session.Loot, factor))), tone: SummaryTone.Highlight),
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummarySuppliesLine"], FormatGold(Scale(request.EffectiveSupplies, factor))), tone: SummaryTone.Danger)
            ]);

            if(request.SessionImbuementCost > 0)
            {
                lines.Add(SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryImbuementLine"], FormatGold(Scale(request.SessionImbuementCost, factor))), tone: SummaryTone.Warning));
            }

            if(!string.IsNullOrWhiteSpace(request.HuntingPlaceName))
            {
                lines.Add(SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryPlaceLine"], request.HuntingPlaceName), tone: SummaryTone.Muted));
            }

            LootInsightSections lootInsights = request.Preset == HuntSummaryTemplatePreset.Detailed && session.LootItems.Count > 0
                ? BuildLootInsights(session.LootItems, factor)
                : LootInsightSections.Empty;

            if(request.Preset == HuntSummaryTemplatePreset.Detailed)
            {
                RespawnProfileSummary profile = BuildRespawnProfile(request.EffectiveXpPerHour, request.EffectiveBalance, effectiveDuration, lootInsights);

                lines.Add(SummaryLine.Blank());
                lines.Add(SummaryLine.Section(_localizationService["Analyzer_SummaryRespawnProfileTitle"] + baselineSuffix));
                lines.Add(SummaryLine.Value(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        _localizationService["Analyzer_SummaryRespawnFocusLine"],
                        _localizationService[profile.FocusKey]),
                    tone: profile.FocusTone));
                lines.Add(SummaryLine.Value(
                    string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryBalancePerHourLine"], FormatGold(profile.BalancePerHour)),
                    tone: profile.BalancePerHour >= 0 ? SummaryTone.Success : SummaryTone.Danger));

                if(lootInsights.ScaledTrackedValue > 0)
                {
                    lines.Add(SummaryLine.Value(
                        string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryTrackedItemValueLine"], FormatGold(lootInsights.ScaledTrackedValue)),
                        tone: SummaryTone.Highlight));
                    lines.Add(SummaryLine.Value(
                        string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryHighValueShareLine"], FormatPercent(lootInsights.HighValueSharePercent)),
                        tone: SummaryTone.Highlight));
                    lines.Add(SummaryLine.Value(
                        string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryCommonDropShareLine"], FormatPercent(lootInsights.CommonDropSharePercent)),
                        tone: SummaryTone.Warning));

                    if(lootInsights.BestValueDensityItem != null)
                    {
                        lines.Add(SummaryLine.Value(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                _localizationService["Analyzer_SummaryBestDensityItemLine"],
                                lootInsights.BestValueDensityItem.DisplayName,
                                FormatGoldPerOz(lootInsights.BestValueDensityItem.ValuePerOz)),
                            tone: SummaryTone.Highlight));
                    }
                }
            }

            if(request.Preset == HuntSummaryTemplatePreset.Detailed && session.KilledMonsters.Count > 0)
            {
                lines.Add(SummaryLine.Blank());
                lines.Add(SummaryLine.Section(_localizationService["Analyzer_SummaryTopMonstersTitle"] + baselineSuffix));

                foreach(HuntMonsterEntry monster in session.KilledMonsters.OrderByDescending(x => x.Amount).ThenBy(x => x.MonsterName).Take(5))
                {
                    lines.Add(SummaryLine.Value(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            _localizationService["Analyzer_SummaryMonsterEntry"],
                            monster.MonsterName,
                            FormatScaledAmount(monster.Amount * factor)),
                        includeMonsterIcons ? TryResolveMonsterImageUri(monster.MonsterName) : null));
                }
            }

            if(request.Preset == HuntSummaryTemplatePreset.Detailed && session.LootItems.Count > 0)
            {
                if(lootInsights.TopItems.Count > 0)
                {
                    lines.Add(SummaryLine.Blank());
                    lines.Add(SummaryLine.Section(_localizationService["Analyzer_SummaryTopItemsTitle"] + baselineSuffix));

                    foreach(LootInsightItem item in lootInsights.TopItems)
                    {
                        lines.Add(SummaryLine.Value(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                _localizationService["Analyzer_SummaryTopItemEntry"],
                                item.DisplayName,
                                FormatScaledAmount(item.ScaledAmount),
                                FormatGold(item.ScaledTotalValue),
                                FormatGoldPerOz(item.ValuePerOz)),
                            tone: SummaryTone.Highlight));
                    }
                }

                if(lootInsights.CommonDrops.Count > 0)
                {
                    lines.Add(SummaryLine.Blank());
                    lines.Add(SummaryLine.Section(_localizationService["Analyzer_SummaryCommonDropsTitle"] + baselineSuffix));

                    foreach(LootInsightItem item in lootInsights.CommonDrops)
                    {
                        lines.Add(SummaryLine.Value(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                _localizationService["Analyzer_SummaryCommonDropEntry"],
                                item.DisplayName,
                                FormatScaledAmount(item.ScaledAmount),
                                FormatGold(item.ScaledTotalValue),
                                FormatGold(item.UnitValue)),
                            tone: SummaryTone.Warning));
                    }
                }
            }

            return new SummaryContent(
                _localizationService["Analyzer_SummarySoloTitle"],
                string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummarySubtitleSolo"], request.CharacterName, FormatTimestamp(session.SessionStartTime, session.ImportedAt)),
                lines);
        }

        private SummaryContent BuildTeamContent(HuntSummaryRequest request, TeamHuntSessionEntity session)
        {
            TimeSpan effectiveDuration = ResolveEffectiveDuration(request.EffectiveDuration, session.Duration);
            double factor = request.NormalizeToQuarterHour ? GetQuarterHourFactor(effectiveDuration) : 1d;
            long? rawXpGain = request.RawXpGain.HasValue && request.RawXpGain.Value > 0 ? request.RawXpGain.Value : null;
            RawXpPerHourResult rawXpPerHour = ResolveTeamRawXpPerHour(session.XpPerHour, effectiveDuration, rawXpGain);

            List<SummaryLine> lines =
            [
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryModeLine"], _localizationService["Analyzer_SummaryModeTeam"]), tone: SummaryTone.Muted),
                request.NormalizeToQuarterHour
                    ? SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryBaselineLine"], TargetBaselineMinutes, FormatDuration(effectiveDuration)), tone: SummaryTone.Muted)
                    : SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryDurationLine"], FormatDuration(effectiveDuration)), tone: SummaryTone.Muted),
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryMembersLine"], session.Members.Count), tone: SummaryTone.Muted),
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryXpPerHourLine"], FormatNumber(session.XpPerHour)), tone: SummaryTone.Info),
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryXpGainLine"], FormatNumber(Scale(session.XpGain, factor))), tone: SummaryTone.Info)
            ];

            if(request.ShowRawXp && rawXpPerHour.IsAvailable && rawXpPerHour.Value > 0)
            {
                string rawXpKey = rawXpPerHour.IsEstimated
                    ? "Analyzer_SummaryRawXpPerHourEstimatedLine"
                    : "Analyzer_SummaryRawXpPerHourLine";
                lines.Add(SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService[rawXpKey], FormatNumber(rawXpPerHour.Value)), tone: SummaryTone.Info));
            }
            else if(request.ShowRawXp)
            {
                lines.Add(SummaryLine.Value(_localizationService["Analyzer_SummaryRawXpPerHourUnavailableLine"], tone: SummaryTone.Muted));
            }

            lines.AddRange(
            [
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryBalanceLine"], FormatGold(Scale(session.TotalBalance, factor))), tone: session.TotalBalance >= 0 ? SummaryTone.Success : SummaryTone.Danger),
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryLootLine"], FormatGold(Scale(session.TotalLoot, factor))), tone: SummaryTone.Highlight),
                SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummarySuppliesLine"], FormatGold(Scale(session.TotalSupplies, factor))), tone: SummaryTone.Danger)
            ]);

            if(request.Preset == HuntSummaryTemplatePreset.Detailed && session.Members.Count > 0)
            {
                lines.Add(SummaryLine.Blank());
                lines.Add(SummaryLine.Section(_localizationService["Analyzer_SummaryMemberBreakdownTitle"]));

                foreach(TeamHuntMemberEntity member in session.Members.OrderByDescending(x => x.Damage).ThenBy(x => x.Name))
                {
                    lines.Add(SummaryLine.Value(string.Format(
                        CultureInfo.CurrentCulture,
                        _localizationService["Analyzer_SummaryMemberEntry"],
                        member.Name,
                        FormatGold(Scale(member.Balance, factor)),
                        FormatNumber(Scale(member.Damage, factor))), tone: member.Balance >= 0 ? SummaryTone.Success : SummaryTone.Danger));
                }
            }

            if(request.Preset == HuntSummaryTemplatePreset.Detailed && !request.NormalizeToQuarterHour && request.Transfers.Count > 0)
            {
                lines.Add(SummaryLine.Blank());
                lines.Add(SummaryLine.Section(_localizationService["Analyzer_SummaryTransfersTitle"]));

                foreach(HuntSummaryTransfer transfer in request.Transfers.OrderByDescending(x => x.Amount).ThenBy(x => x.FromName).ThenBy(x => x.ToName))
                {
                    lines.Add(SummaryLine.Value(string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryTransferEntry"], transfer.FromName, transfer.ToName, FormatGold(transfer.Amount)), tone: SummaryTone.Success));
                }
            }
            else if(request.Preset == HuntSummaryTemplatePreset.Detailed && request.NormalizeToQuarterHour && request.Transfers.Count > 0)
            {
                lines.Add(SummaryLine.Blank());
                lines.Add(SummaryLine.Value(_localizationService["Analyzer_SummaryTransfersOmitted"], tone: SummaryTone.Muted));
            }

            return new SummaryContent(
                _localizationService["Analyzer_SummaryTeamTitle"],
                string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummarySubtitleTeam"], request.CharacterName, FormatTimestamp(session.SessionStartTime, session.ImportedAt)),
                lines);
        }

        private void RenderToPng(SummaryContent content, string filePath)
        {
            Directory.CreateDirectory(_paths.SummariesDirectory);

            int width = 1340;
            double padding = 54;
            double titleFontSize = 38;
            double subtitleFontSize = 18;
            double bodyFontSize = 24;
            double lineHeight = 40;
            double iconLineHeight = 66;
            double blankLineHeight = 20;
            double iconSize = 52;
            double iconGap = 18;
            double contentHeight = 0;

            foreach(SummaryLine line in content.Lines)
            {
                contentHeight += line.Kind switch
                {
                    SummaryLineKind.Blank => blankLineHeight,
                    _ when !string.IsNullOrWhiteSpace(line.IconUri) => iconLineHeight,
                    _ => lineHeight
                };
            }

            int height = (int)Math.Ceiling(padding + 48 + subtitleFontSize + 46 + contentHeight + padding + 24);
            RenderTargetBitmap bitmap = new(new PixelSize(width, height), new Vector(96, 96));

            using(DrawingContext context = bitmap.CreateDrawingContext(true))
            {
                IBrush background = new SolidColorBrush(Color.Parse("#0B0D12"));
                IBrush cardBackground = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    [
                        new GradientStop(Color.Parse("#171B25"), 0),
                        new GradientStop(Color.Parse("#11161F"), 1)
                    ]
                };
                IBrush panelBackground = new SolidColorBrush(Color.Parse("#1A2030"));
                IBrush accentBrush = new SolidColorBrush(Color.Parse("#E6B84B"));
                IBrush subtitleBrush = new SolidColorBrush(Color.Parse("#98A2B5"));
                Pen borderPen = new(new SolidColorBrush(Color.Parse("#2A3245")), 2);

                context.FillRectangle(background, new Rect(0, 0, width, height));
                context.DrawRectangle(cardBackground, borderPen, new Rect(24, 24, width - 48, height - 48));
                context.FillRectangle(accentBrush, new Rect(24, 24, width - 48, 8));
                context.DrawRectangle(panelBackground, null, new Rect(54, 54, width - 108, height - 108));

                double y = padding;
                context.DrawText(CreateText(content.Title, titleFontSize, FontWeight.Bold, Brushes.White, TitleFontFamily), new Point(padding + 24, y));
                y += 48;
                context.DrawText(CreateText(content.Subtitle, subtitleFontSize, FontWeight.Medium, subtitleBrush, BodyFontFamily), new Point(padding + 24, y));
                y += 40;
                context.FillRectangle(new SolidColorBrush(Color.Parse("#273146")), new Rect(padding + 24, y, width - (padding + 24) * 2, 2));
                y += 22;

                foreach(SummaryLine line in content.Lines)
                {
                    if(line.Kind == SummaryLineKind.Blank)
                    {
                        y += blankLineHeight;
                        continue;
                    }

                    bool isSectionLine = line.Kind == SummaryLineKind.Section;
                    double currentLineHeight = !string.IsNullOrWhiteSpace(line.IconUri) ? iconLineHeight : lineHeight;
                    double textX = padding + 24;

                    if(!string.IsNullOrWhiteSpace(line.IconUri) &&
                       TryLoadBitmap(line.IconUri!, out Bitmap? bitmapIcon) &&
                       bitmapIcon != null)
                    {
                        using(bitmapIcon)
                        {
                            context.DrawImage(bitmapIcon, new Rect(0, 0, bitmapIcon.Size.Width, bitmapIcon.Size.Height), new Rect(textX, y + (currentLineHeight - iconSize) / 2, iconSize, iconSize));
                        }
                        textX += iconSize + iconGap;
                    }

                    if(isSectionLine)
                    {
                        context.FillRectangle(accentBrush, new Rect(textX, y + 10, 6, currentLineHeight - 20));
                        context.DrawText(
                            CreateText(line.Text, bodyFontSize - 1, FontWeight.SemiBold, ResolveToneBrush(SummaryTone.Highlight)),
                            new Point(textX + 18, y + (currentLineHeight - lineHeight) / 2));
                    }
                    else if(TrySplitMetricText(line.Text, out string labelText, out string valueText))
                    {
                        FormattedText label = CreateText(labelText, bodyFontSize, FontWeight.Medium, subtitleBrush);
                        FormattedText value = CreateText(valueText, bodyFontSize, FontWeight.SemiBold, ResolveToneBrush(line.Tone));
                        double contentRight = width - padding - 86;
                        double valueX = Math.Max(textX + label.Width + 22, contentRight - value.Width);
                        context.DrawText(label, new Point(textX, y + (currentLineHeight - lineHeight) / 2));
                        context.DrawText(value, new Point(valueX, y + (currentLineHeight - lineHeight) / 2));
                    }
                    else
                    {
                        context.DrawText(
                            CreateText(line.Text, bodyFontSize, FontWeight.Normal, ResolveToneBrush(line.Tone)),
                            new Point(textX, y + (currentLineHeight - lineHeight) / 2));
                    }
                    y += currentLineHeight;
                }
            }

            bitmap.Save(filePath);
        }

        private FormattedText CreateText(string text, double fontSize, FontWeight fontWeight, IBrush foreground, FontFamily? fontFamily = null)
        {
            return new FormattedText(
                text,
                _localizationService.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(fontFamily ?? BodyFontFamily, FontStyle.Normal, fontWeight),
                fontSize,
                foreground);
        }

        private static string BuildPlainText(SummaryContent content)
        {
            List<string> lines = [content.Title, content.Subtitle, string.Empty];
            lines.AddRange(content.Lines.Where(x => x.Kind != SummaryLineKind.Blank).Select(x => x.Text));
            return string.Join(Environment.NewLine, lines);
        }

        private string BuildDiscordText(SummaryContent content)
        {
            List<string> lines =
            [
                ApplyAnsi(content.Title, SummaryTone.Highlight),
                ApplyAnsi(content.Subtitle, SummaryTone.Muted),
                ApplyAnsi(new string('-', 42), SummaryTone.Muted),
                string.Empty
            ];

            foreach(SummaryLine line in content.Lines)
            {
                if(line.Kind == SummaryLineKind.Blank)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                if(line.Kind == SummaryLineKind.Section)
                {
                    lines.Add(ApplyAnsi($"▶ {line.Text}", SummaryTone.Highlight));
                    continue;
                }

                lines.Add(ApplyAnsiValueLine(line.Text, line.Tone));
            }

            return $"```ansi{Environment.NewLine}{string.Join(Environment.NewLine, lines)}{Environment.NewLine}```";
        }

        private string FormatTimestamp(DateTimeOffset primary, DateTimeOffset fallback)
        {
            DateTimeOffset timestamp = primary.Year > 2000 ? primary : fallback;
            return timestamp.ToLocalTime().ToString("g", _localizationService.CurrentCulture);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return duration.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        }

        private string FormatNumber(long value)
        {
            return value.ToString("N0", _localizationService.CurrentCulture);
        }

        private string FormatGold(long value)
        {
            return string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryGoldValue"], FormatNumber(value));
        }

        private string FormatGoldPerOz(decimal valuePerOz)
        {
            return string.Format(CultureInfo.CurrentCulture, _localizationService["Analyzer_SummaryGoldPerOzValue"], valuePerOz.ToString("N0", _localizationService.CurrentCulture));
        }

        private string FormatPercent(decimal value)
        {
            return value.ToString("N0", _localizationService.CurrentCulture) + "%";
        }

        private string FormatScaledAmount(double amount)
        {
            double rounded = Math.Round(amount, 1, MidpointRounding.AwayFromZero);
            bool hasFraction = Math.Abs(rounded - Math.Round(rounded, 0, MidpointRounding.AwayFromZero)) > 0.05d;
            return hasFraction
                ? rounded.ToString("N1", _localizationService.CurrentCulture)
                : Math.Round(rounded, 0, MidpointRounding.AwayFromZero).ToString("N0", _localizationService.CurrentCulture);
        }

        private LootInsightSections BuildLootInsights(IReadOnlyList<HuntLootEntry> lootItems, double factor)
        {
            if(lootItems.Count == 0)
            {
                return LootInsightSections.Empty;
            }

            List<SummaryLootProjection> groupedLoot = lootItems
                .Where(item => item.Amount > 0 && !string.IsNullOrWhiteSpace(item.ItemName))
                .GroupBy(item => NormalizeItemName(item.ItemName), StringComparer.Ordinal)
                .Select(group => new SummaryLootProjection(
                    group.Key,
                    group.First().ItemName.Trim(),
                    group.Sum(item => item.Amount)))
                .ToList();

            if(groupedLoot.Count == 0)
            {
                return LootInsightSections.Empty;
            }

            Dictionary<string, SummaryItemMetadata> metadataMap = ResolveItemMetadata(groupedLoot.Select(item => item.NormalizedName));

            List<LootInsightItem> detailedItems = groupedLoot
                .Where(item => metadataMap.ContainsKey(item.NormalizedName) &&
                               metadataMap[item.NormalizedName].Value > 0 &&
                               !SummaryExcludedItems.Contains(item.NormalizedName))
                .Select(item =>
                {
                    SummaryItemMetadata metadata = metadataMap[item.NormalizedName];
                    long totalValue = metadata.Value * item.Amount;
                    double scaledAmount = item.Amount * factor;
                    long scaledTotalValue = Scale(totalValue, factor);
                    decimal valuePerOz = metadata.WeightOz.HasValue && metadata.WeightOz.Value > 0
                        ? decimal.Round(metadata.Value / metadata.WeightOz.Value, 0, MidpointRounding.AwayFromZero)
                        : 0m;

                    return new LootInsightItem(
                        item.DisplayName,
                        scaledAmount,
                        scaledTotalValue,
                        metadata.Value,
                        metadata.WeightOz,
                        valuePerOz);
                })
                .ToList();

            List<LootInsightItem> topItems = detailedItems
                .Where(item => item.ValuePerOz > 0m)
                .OrderByDescending(item => item.ValuePerOz)
                .ThenByDescending(item => item.ScaledTotalValue)
                .ThenBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .Take(TopItemLimit)
                .ToList();

            List<LootInsightItem> commonDrops = detailedItems
                .Where(item => item.UnitValue > 0 && item.UnitValue < CommonDropValueThreshold)
                .OrderByDescending(item => item.ScaledAmount)
                .ThenByDescending(item => item.ScaledTotalValue)
                .ThenBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .Take(TopItemLimit)
                .ToList();

            long scaledTrackedValue = detailedItems.Sum(item => item.ScaledTotalValue);
            long scaledHighValueTotal = detailedItems
                .Where(item => item.UnitValue >= CommonDropValueThreshold)
                .Sum(item => item.ScaledTotalValue);
            long scaledCommonDropTotal = detailedItems
                .Where(item => item.UnitValue > 0 && item.UnitValue < CommonDropValueThreshold)
                .Sum(item => item.ScaledTotalValue);
            decimal highValueSharePercent = scaledTrackedValue > 0
                ? decimal.Round((decimal)scaledHighValueTotal * 100m / scaledTrackedValue, 0, MidpointRounding.AwayFromZero)
                : 0m;
            decimal commonDropSharePercent = scaledTrackedValue > 0
                ? decimal.Round((decimal)scaledCommonDropTotal * 100m / scaledTrackedValue, 0, MidpointRounding.AwayFromZero)
                : 0m;

            return new LootInsightSections(
                topItems,
                commonDrops,
                scaledTrackedValue,
                highValueSharePercent,
                commonDropSharePercent,
                topItems.FirstOrDefault());
        }

        private RespawnProfileSummary BuildRespawnProfile(
            long effectiveXpPerHour,
            long effectiveBalance,
            TimeSpan effectiveDuration,
            LootInsightSections lootInsights)
        {
            long balancePerHour = effectiveDuration.TotalHours > 0.01d
                ? (long)Math.Round(effectiveBalance / effectiveDuration.TotalHours, MidpointRounding.AwayFromZero)
                : effectiveBalance;

            string focusKey;
            SummaryTone focusTone;

            if(balancePerHour >= 350_000 || (balancePerHour >= 250_000 && lootInsights.HighValueSharePercent >= 55m))
            {
                focusKey = "Analyzer_SummaryRespawnFocusLoot";
                focusTone = SummaryTone.Success;
            }
            else if(balancePerHour <= 175_000 || (effectiveXpPerHour >= 4_000_000 && balancePerHour <= 250_000))
            {
                focusKey = "Analyzer_SummaryRespawnFocusXp";
                focusTone = SummaryTone.Info;
            }
            else
            {
                focusKey = "Analyzer_SummaryRespawnFocusBalanced";
                focusTone = SummaryTone.Highlight;
            }

            return new RespawnProfileSummary(balancePerHour, focusKey, focusTone);
        }

        private Dictionary<string, SummaryItemMetadata> ResolveItemMetadata(IEnumerable<string> normalizedNames)
        {
            List<string> requestedNames = normalizedNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if(requestedNames.Count == 0 || _dbFactory == null)
            {
                return new Dictionary<string, SummaryItemMetadata>(StringComparer.Ordinal);
            }

            List<string> missingNames;
            lock(_itemMetadataGate)
            {
                missingNames = requestedNames
                    .Where(name => !_itemMetadataCache.ContainsKey(name))
                    .ToList();
            }

            if(missingNames.Count > 0)
            {
                using AppDbContext db = _dbFactory.CreateDbContext();
                List<SummaryItemMetadata> loaded = db.Items
                    .AsNoTracking()
                    .Where(item => missingNames.Contains(item.NormalizedName))
                    .Select(item => new
                    {
                        item.NormalizedName,
                        item.Name,
                        item.Value,
                        item.NpcValue,
                        item.NpcPrice,
                        item.WeightOz
                    })
                    .AsEnumerable()
                    .Select(item => new SummaryItemMetadata(
                        item.NormalizedName,
                        item.Name,
                        ItemValueResolver.GetEffectiveValue(item.Value, item.NpcValue, item.NpcPrice),
                        item.WeightOz))
                    .ToList();

                lock(_itemMetadataGate)
                {
                    foreach(SummaryItemMetadata item in loaded)
                    {
                        _itemMetadataCache[item.NormalizedName] = item;
                    }
                }
            }

            lock(_itemMetadataGate)
            {
                return requestedNames
                    .Where(name => _itemMetadataCache.ContainsKey(name))
                    .ToDictionary(name => name, name => _itemMetadataCache[name], StringComparer.Ordinal);
            }
        }

        internal static long Scale(long value, double factor)
        {
            return (long)Math.Round(value * factor, MidpointRounding.AwayFromZero);
        }

        internal static double GetQuarterHourFactor(TimeSpan duration)
        {
            return duration.TotalMinutes <= 0.01 ? 1d : TargetBaselineMinutes / duration.TotalMinutes;
        }

        internal static long? TryExtractRawXpGain(string? rawInput)
        {
            if(string.IsNullOrWhiteSpace(rawInput))
            {
                return null;
            }

            foreach(string label in XpGainLabels)
            {
                Match match = Regex.Match(rawInput, $@"Raw\s+{Regex.Escape(label)}\s*:\s*(?<val>-?[\d,.]+)", RegexOptions.IgnoreCase);
                if(match.Success && TryParseLongValue(match.Groups["val"].Value, out long value))
                {
                    return value;
                }
            }

            return null;
        }

        private static TimeSpan ResolveEffectiveDuration(TimeSpan? requestedDuration, TimeSpan fallbackDuration)
        {
            if(requestedDuration.HasValue && requestedDuration.Value > TimeSpan.Zero)
            {
                return requestedDuration.Value;
            }

            return fallbackDuration > TimeSpan.Zero
                ? fallbackDuration
                : TimeSpan.Zero;
        }

        private static RawXpPerHourResult ResolveSoloRawXpPerHour(
            long displayedXpPerHour,
            TimeSpan duration,
            long? rawXpGain,
            bool allowEstimatedRawXp,
            bool isDoubleXp,
            int? xpBoostPercent,
            int? xpBoostActiveMinutes,
            int? customXpRatePercent)
        {
            if(rawXpGain.HasValue && duration.TotalHours > 0.01)
            {
                return new RawXpPerHourResult((long)Math.Round(rawXpGain.Value / duration.TotalHours, MidpointRounding.AwayFromZero), false, true);
            }

            if(!allowEstimatedRawXp || displayedXpPerHour <= 0)
            {
                return new RawXpPerHourResult(0, false, false);
            }

            double displayedFactor = ResolveDisplayedXpFactor(duration, isDoubleXp, xpBoostPercent, xpBoostActiveMinutes, customXpRatePercent);
            if(displayedFactor <= 0.01d)
            {
                return new RawXpPerHourResult(0, false, false);
            }

            long estimated = EstimateRawXpPerHour(displayedXpPerHour, displayedFactor);
            return new RawXpPerHourResult(estimated, true, estimated > 0);
        }

        private static RawXpPerHourResult ResolveTeamRawXpPerHour(long displayedXpPerHour, TimeSpan duration, long? rawXpGain)
        {
            if(rawXpGain.HasValue && duration.TotalHours > 0.01)
            {
                return new RawXpPerHourResult((long)Math.Round(rawXpGain.Value / duration.TotalHours, MidpointRounding.AwayFromZero), false, true);
            }

            return new RawXpPerHourResult(0, false, false);
        }

        private static long EstimateRawXpPerHour(long displayedXpPerHour, double displayedFactor)
        {
            if(displayedXpPerHour <= 0 || displayedFactor <= 0.01d)
            {
                return 0;
            }

            return (long)Math.Round(displayedXpPerHour / displayedFactor, MidpointRounding.AwayFromZero);
        }

        private static double ResolveDisplayedXpFactor(
            TimeSpan duration,
            bool isDoubleXp,
            int? xpBoostPercent,
            int? xpBoostActiveMinutes,
            int? customXpRatePercent)
        {
            double baseFactor = (customXpRatePercent.HasValue && customXpRatePercent.Value > 0
                ? customXpRatePercent.Value
                : DefaultSoloXpRatePercent) / 100d;
            if(isDoubleXp)
            {
                baseFactor *= 2d;
            }

            if(!xpBoostPercent.HasValue || xpBoostPercent.Value <= 0)
            {
                return baseFactor;
            }

            double boostFactor = baseFactor + (xpBoostPercent.Value / 100d);
            double effectiveMinutes = duration.TotalMinutes;
            if(effectiveMinutes <= 0.01d)
            {
                return baseFactor;
            }

            double boostedMinutes = xpBoostActiveMinutes.HasValue && xpBoostActiveMinutes.Value > 0
                ? Math.Min(effectiveMinutes, xpBoostActiveMinutes.Value)
                : effectiveMinutes;

            double normalMinutes = Math.Max(0d, effectiveMinutes - boostedMinutes);
            return ((boostedMinutes * boostFactor) + (normalMinutes * baseFactor)) / effectiveMinutes;
        }

        private static bool TryParseLongValue(string value, out long parsed)
        {
            string normalized = Regex.Replace(value, @"[^\d-]", string.Empty);
            return long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
        }

        private static string NormalizeItemName(string? itemName)
        {
            return itemName?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        private string? TryResolveMonsterImageUri(string monsterName)
        {
            return _monsterImageCatalogService.TryResolveImageUri(null, monsterName, out string imageUri)
                ? imageUri
                : null;
        }

        private static bool TryLoadBitmap(string pathOrUri, out Bitmap? bitmap)
        {
            bitmap = null;

            try
            {
                if(Uri.TryCreate(pathOrUri, UriKind.Absolute, out Uri? uri) && string.Equals(uri.Scheme, "avares", StringComparison.OrdinalIgnoreCase))
                {
                    using Stream stream = AssetLoader.Open(uri);
                    bitmap = new Bitmap(stream);
                    return true;
                }

                if(File.Exists(pathOrUri))
                {
                    using FileStream stream = File.OpenRead(pathOrUri);
                    bitmap = new Bitmap(stream);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private sealed record SummaryContent(string Title, string Subtitle, IReadOnlyList<SummaryLine> Lines);
        private sealed record SummaryItemMetadata(string NormalizedName, string DisplayName, long Value, decimal? WeightOz);
        private sealed record SummaryLootProjection(string NormalizedName, string DisplayName, int Amount);
        private sealed record LootInsightItem(
            string DisplayName,
            double ScaledAmount,
            long ScaledTotalValue,
            long UnitValue,
            decimal? WeightOz,
            decimal ValuePerOz);
        private sealed record LootInsightSections(
            IReadOnlyList<LootInsightItem> TopItems,
            IReadOnlyList<LootInsightItem> CommonDrops,
            long ScaledTrackedValue,
            decimal HighValueSharePercent,
            decimal CommonDropSharePercent,
            LootInsightItem? BestValueDensityItem)
        {
            public static LootInsightSections Empty { get; } = new(Array.Empty<LootInsightItem>(), Array.Empty<LootInsightItem>(), 0, 0m, 0m, null);
        }

        private static string ApplyAnsi(string text, SummaryTone tone)
        {
            string code = tone switch
            {
                SummaryTone.Highlight => "1;33",
                SummaryTone.Info => "1;36",
                SummaryTone.Warning => "1;35",
                SummaryTone.Success => "1;32",
                SummaryTone.Danger => "1;31",
                SummaryTone.Muted => "0;37",
                _ => "0;37"
            };

            return $"\u001b[{code}m{text}\u001b[0m";
        }

        private static string ApplyAnsiValueLine(string text, SummaryTone tone)
        {
            if(!TrySplitMetricText(text, out string labelText, out string valueText))
            {
                return ApplyAnsi(text, tone);
            }

            return $"{ApplyAnsi(labelText.PadRight(24), SummaryTone.Muted)} {ApplyAnsi(valueText, tone)}";
        }

        private static bool TrySplitMetricText(string text, out string labelText, out string valueText)
        {
            int separatorIndex = text.IndexOf(':');
            if(separatorIndex <= 0 || separatorIndex >= text.Length - 1 || text.StartsWith("- ", StringComparison.Ordinal))
            {
                labelText = string.Empty;
                valueText = string.Empty;
                return false;
            }

            labelText = text[..(separatorIndex + 1)];
            valueText = text[(separatorIndex + 1)..].TrimStart();
            return !string.IsNullOrWhiteSpace(valueText);
        }

        private static IBrush ResolveToneBrush(SummaryTone tone)
        {
            return tone switch
            {
                SummaryTone.Highlight => new SolidColorBrush(Color.Parse("#F2D37A")),
                SummaryTone.Info => new SolidColorBrush(Color.Parse("#6EE7F9")),
                SummaryTone.Warning => new SolidColorBrush(Color.Parse("#F7B955")),
                SummaryTone.Success => new SolidColorBrush(Color.Parse("#72E0A2")),
                SummaryTone.Danger => new SolidColorBrush(Color.Parse("#FF7A7A")),
                SummaryTone.Muted => new SolidColorBrush(Color.Parse("#A5AFBF")),
                _ => Brushes.White
            };
        }

        private sealed record SummaryLine(SummaryLineKind Kind, string Text, string? IconUri = null, SummaryTone Tone = SummaryTone.Default)
        {
            public static SummaryLine Value(string text, string? iconUri = null, SummaryTone tone = SummaryTone.Default) => new(SummaryLineKind.Value, text, iconUri, tone);

            public static SummaryLine Section(string text) => new(SummaryLineKind.Section, text, null, SummaryTone.Highlight);

            public static SummaryLine Blank() => new(SummaryLineKind.Blank, string.Empty);
        }

        private enum SummaryLineKind
        {
            Value,
            Section,
            Blank
        }

        private sealed record RawXpPerHourResult(long Value, bool IsEstimated, bool IsAvailable);
        private sealed record RespawnProfileSummary(long BalancePerHour, string FocusKey, SummaryTone FocusTone);

        private enum SummaryTone
        {
            Default,
            Highlight,
            Info,
            Warning,
            Success,
            Danger,
            Muted
        }
    }
}
