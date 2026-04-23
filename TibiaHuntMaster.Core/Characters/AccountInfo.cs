namespace TibiaHuntMaster.Core.Characters
{
    /// <summary>
    ///     Optional 1:1 account information for a character.
    /// </summary>
    public sealed class AccountInfo
    {
        // NOTE: Keep types aligned with your EF entity. Strings are lenient if API formats vary.
        public string Created { get; set; } = string.Empty;

        public string LoyaltyTitle { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;
    }
}