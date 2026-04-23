namespace TibiaHuntMaster.Core.TibiaPal
{
    /// <summary>
    ///     Repräsentiert eine Jagd-Empfehlung von TibiaPal.
    /// </summary>
    public sealed record TibiaPalHuntingSpot(
        string Name,
        int MinLevel,
        string Vocation,
        string ExpInfo, // z.B. "35k" (wir lassen es als String, da "k" Suffixe typisch sind)
        string LootInfo, // z.B. "5k", "-5k"
        string WeaponType, // NEU: z.B. "Physical"
        string? YouTubeUrl
    );
}