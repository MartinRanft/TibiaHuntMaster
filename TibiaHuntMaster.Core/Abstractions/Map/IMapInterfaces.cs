namespace TibiaHuntMaster.Core.Abstractions.Map
{
    // Backward-compatible shim. Prefer the top-level interfaces in this namespace.
    public static class MapInterfaces
    {
        public interface IMinimapTileCatalog : global::TibiaHuntMaster.Core.Abstractions.Map.IMinimapTileCatalog;
        public interface IMapSectionService : global::TibiaHuntMaster.Core.Abstractions.Map.IMapSectionService;
        public interface IMinimapMarkerService : global::TibiaHuntMaster.Core.Abstractions.Map.IMinimapMarkerService;
        public interface IMonsterSpawnQueryService : global::TibiaHuntMaster.Core.Abstractions.Map.IMonsterSpawnQueryService;
    }
}
