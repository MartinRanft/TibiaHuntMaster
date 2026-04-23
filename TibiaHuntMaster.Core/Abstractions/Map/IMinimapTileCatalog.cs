namespace TibiaHuntMaster.Core.Abstractions.Map
{
    public interface IMinimapTileCatalog
    {
        bool TryGetTilePath(Core.Map.Map.MinimapLayer layer, int absX, int absY, byte z, out string filePath);
        Core.Map.Map.MapBounds GetKnownBounds(byte z);
    }
}
