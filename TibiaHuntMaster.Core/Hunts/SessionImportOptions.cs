namespace TibiaHuntMaster.Core.Hunts
{
    /// <summary>
    ///     Parameter für den Import einer Jagd-Session.
    /// </summary>
    public sealed record SessionImportOptions(
        string RawText,
        string CharacterName,
        bool IsDoubleXp,
        bool IsDoubleLoot,
        bool IsRapidRespawn,
        string? Notes,
        int? XpBoostPercent = null,
        int? XpBoostActiveMinutes = null,
        int? CustomXpRatePercent = null
    );
}
