using TibiaHuntMaster.Core.Characters;

namespace TibiaHuntMaster.Core.Abstractions.TibiaData
{
    public interface ICharacterService
    {
        Task<Character> ImportFromTibiaDataAsync(string characterName, CancellationToken ct = default);
        Task SaveAsync(Character character, CancellationToken ct = default);
        Task<IReadOnlyList<Character>> ListAsync(CancellationToken ct = default);
        Task<Character?> GetByNameAsync(string name, CancellationToken ct = default);
    }
}