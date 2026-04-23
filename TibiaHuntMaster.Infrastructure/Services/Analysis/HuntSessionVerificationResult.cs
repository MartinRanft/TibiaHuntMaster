namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public sealed record HuntSessionVerificationResult(
        long ReportedLoot,
        long CalculatedLoot,
        long LootDelta,
        bool HasLootMismatch,
        bool CanApplyLootCorrection,
        long? ReportedRawXpGain,
        long CalculatedRawXpGain,
        long? RawXpDelta,
        bool HasRawXpMismatch,
        long ReportedXpGain,
        long CalculatedXpGain,
        long XpDelta,
        long ReportedXpPerHour,
        long CalculatedXpPerHour,
        bool HasXpMismatch,
        bool IsXpEstimated,
        bool CanApplyXpCorrection,
        IReadOnlyList<string> UnmatchedLootItems,
        IReadOnlyList<string> UnmatchedCreatures)
    {
        public bool HasAnyIssues => HasLootMismatch || HasRawXpMismatch || HasXpMismatch;

        public bool HasAnyUnmatchedData => UnmatchedLootItems.Count > 0 || UnmatchedCreatures.Count > 0;
    }
}
