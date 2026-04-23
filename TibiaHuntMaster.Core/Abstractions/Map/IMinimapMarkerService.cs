namespace TibiaHuntMaster.Core.Abstractions.Map
{
    public interface IMinimapMarkerService
    {
        IReadOnlyList<Core.Map.Map.MinimapMarker> GetAllMarkers();

        IReadOnlyList<Core.Map.Map.MinimapMarker> GetMarkersInBounds(
            int minX,
            int minY,
            int maxX,
            int maxY,
            byte z);
    }
}
