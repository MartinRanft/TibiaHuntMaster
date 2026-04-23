namespace TibiaHuntMaster.Core.Abstractions.Map
{
    public interface IMonsterSpawnQueryService
    {
        Task<IReadOnlyList<Core.Map.Map.MonsterSpawnMarker>> GetSpawnsInBoundsAsync(
            int minX,
            int minY,
            int maxX,
            int maxY,
            byte z,
            string? monsterName = null,
            int? maxResults = null,
            CancellationToken ct = default);

        Task<IReadOnlyList<string>> SearchMonsterNamesAsync(
            string query,
            int limit = 12,
            CancellationToken ct = default);
    }
}
