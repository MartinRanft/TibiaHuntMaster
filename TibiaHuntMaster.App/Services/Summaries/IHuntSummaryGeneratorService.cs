using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.App.Services.Summaries
{
    public enum HuntSummaryFormat
    {
        Discord,
        Text,
        Image
    }

    public enum HuntSummaryTemplatePreset
    {
        Compact,
        Detailed
    }

    public sealed record HuntSummaryTransfer(string FromName, string ToName, long Amount);

    public sealed record HuntSummaryRequest(
        string CharacterName,
        HuntSummaryFormat Format,
        HuntSummaryTemplatePreset Preset,
        bool NormalizeToQuarterHour,
        bool ShowRawXp,
        HuntSessionEntity? SoloSession,
        TeamHuntSessionEntity? TeamSession,
        IReadOnlyList<HuntSummaryTransfer> Transfers,
        string? HuntingPlaceName,
        long EffectiveXpPerHour,
        long EffectiveBalance,
        long EffectiveSupplies,
        long SessionImbuementCost,
        long? RawXpGain,
        TimeSpan? EffectiveDuration = null,
        int? XpBoostPercent = null,
        int? XpBoostActiveMinutes = null,
        int? CustomXpRatePercent = null,
        bool AllowEstimatedRawXp = true);

    public interface IHuntSummaryGeneratorService
    {
        string BuildPreviewText(HuntSummaryRequest request);

        string BuildText(HuntSummaryRequest request);

        Task<string> ExportImageAsync(HuntSummaryRequest request, CancellationToken cancellationToken = default);
    }
}
