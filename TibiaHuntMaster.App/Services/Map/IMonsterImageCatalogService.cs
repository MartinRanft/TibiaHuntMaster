namespace TibiaHuntMaster.App.Services.Map
{
    public interface IMonsterImageCatalogService
    {
        string DeathFallbackImageUri { get; }

        string PlayerKillerImageUri { get; }

        Task EnsureCatalogAsync(CancellationToken ct = default);

        bool TryResolveImageUri(int? creatureId, string? monsterName, out string imageUri);
    }
}
