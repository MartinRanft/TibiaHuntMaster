using TibiaHuntMaster.Infrastructure.Services.Content.Models;

namespace TibiaHuntMaster.Infrastructure.Services.Content.Imports.Interfaces
{
    public interface IItemContentImportService
    {
        Task<ContentOperationResult> ImportItemsAsync(CancellationToken ct = default);
        Task<ContentOperationResult> UpdateItemsAsync(CancellationToken ct = default);
        Task<ContentOperationResult> ReInitializeItemsAsync(CancellationToken ct = default);
    }
}
