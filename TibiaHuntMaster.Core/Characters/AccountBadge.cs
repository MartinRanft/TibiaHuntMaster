namespace TibiaHuntMaster.Core.Characters
{
    /// <summary>
    ///     Badge shown on a player's account.
    /// </summary>
    public sealed class AccountBadge
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string IconUrl { get; set; } = string.Empty;
    }
}