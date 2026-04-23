namespace TibiaHuntMaster.Core.Characters
{
    /// <summary>
    ///     Character achievement metadata.
    /// </summary>
    public sealed class Achievement
    {
        public string Name { get; set; } = string.Empty;

        public string Grade { get; set; } = string.Empty;

        public bool Secret { get; set; }
    }
}