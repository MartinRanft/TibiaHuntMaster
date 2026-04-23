using TibiaHuntMaster.Infrastructure.Services.Content.Models;

namespace TibiaHuntMaster.Infrastructure.Services.Content.Imports.Interfaces
{
    public interface IHuntingPlaceContentImportService
    {
        Task<ContentOperationResult> ImportHuntingPlacesAsync(CancellationToken ct = default);
        Task<ContentOperationResult> UpdateHuntingPlacesAsync(CancellationToken ct = default);
        Task<ContentOperationResult> ReInitializeHuntingPlacesAsync(CancellationToken ct = default);
    }
}