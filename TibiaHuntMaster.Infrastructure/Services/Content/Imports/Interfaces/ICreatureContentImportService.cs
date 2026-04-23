using TibiaHuntMaster.Infrastructure.Services.Content.Models;

namespace TibiaHuntMaster.Infrastructure.Services.Content.Imports.Interfaces
{
    public interface ICreatureContentImportService
    {
        Task<ContentOperationResult> ImportCreaturesAsync(CancellationToken ct = default);
        Task<ContentOperationResult> UpdateCreaturesAsync(CancellationToken ct = default);
        Task<ContentOperationResult> ReInitializeCreaturesAsync(CancellationToken ct = default);
    }
}