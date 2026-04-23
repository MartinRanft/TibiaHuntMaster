namespace TibiaHuntMaster.Core.Abstractions.Map
{
    public interface IMapSectionService
    {
        Core.Map.Map.MapSection GetSection(Core.Map.Map.MapSectionRequest request);
    }
}
