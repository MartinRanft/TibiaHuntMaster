using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    /// <summary>
    ///     Service for comparing hunt sessions.
    /// </summary>
    public interface IHuntComparisonService
    {
        /// <summary>
        ///     Compares two hunt sessions and returns the differences.
        /// </summary>
        /// <param name="sessionA">First session.</param>
        /// <param name="sessionB">Second session.</param>
        /// <returns>Comparison result with percentage differences.</returns>
        ComparisonResult Compare(HuntSessionEntity sessionA, HuntSessionEntity sessionB);
    }
}
