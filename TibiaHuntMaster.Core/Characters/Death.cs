namespace TibiaHuntMaster.Core.Characters
{
    /// <summary>
    ///     A recorded death entry for a character.
    /// </summary>
    public sealed class Death
    {
        /// <summary>
        ///     Unique timestamp of the death (UTC).
        /// </summary>
        public DateTimeOffset TimeUtc { get; set; }

        public int Level { get; set; }

        public string Reason { get; set; } = string.Empty;

        /// <summary>
        ///     Raw killers/assists JSON if you want to keep exact API structure.
        /// </summary>
        public string? KillersJson { get; set; }
    }
}