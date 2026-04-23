namespace TibiaHuntMaster.Core.Characters
{
    /// <summary>
    ///     Ownership of a house/guildhall.
    /// </summary>
    public sealed class House
    {
        public int HouseId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Town { get; set; } = string.Empty;

        public string Paid { get; set; } = string.Empty; // e.g. "paid until" raw string from API
    }
}