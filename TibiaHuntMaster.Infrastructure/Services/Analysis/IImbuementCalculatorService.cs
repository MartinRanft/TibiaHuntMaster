using TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    /// <summary>
    ///     Service for calculating imbuement costs and managing imbuement profiles.
    /// </summary>
    public interface IImbuementCalculatorService
    {
        /// <summary>
        ///     Calculates the hourly cost of imbuements for a character.
        /// </summary>
        /// <param name="characterId">Character ID.</param>
        /// <returns>Hourly cost in gold.</returns>
        Task<long> CalculateHourlyCostAsync(int characterId);

        /// <summary>
        ///     Updates the price of an item used in imbuements.
        /// </summary>
        /// <param name="itemId">Item ID.</param>
        /// <param name="price">New price in gold.</param>
        Task UpdateItemPriceAsync(int itemId, long price);

        /// <summary>
        ///     Gets the imbuement profile for a character.
        /// </summary>
        /// <param name="characterId">Character ID.</param>
        /// <returns>Imbuement profile entity.</returns>
        Task<ImbuementProfileEntity> GetProfileAsync(int characterId);
    }
}
