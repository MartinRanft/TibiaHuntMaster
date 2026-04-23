namespace TibiaHuntMaster.Core.Map
{
    public class Map
    {
        public enum MinimapLayer
        {
            Color = 0,
            WaypointCost = 1
        }

        public readonly record struct MapBounds(int MinX, int MinY, int MaxX, int MaxY, byte Z);

        public sealed record MapSectionRequest(
            int CenterX,
            int CenterY,
            byte Z,
            int Width,
            int Height,
            MinimapLayer Layer
        );

        public sealed record MapSectionTile(
            int TileAbsX,
            int TileAbsY,
            byte Z,
            MinimapLayer Layer,
            string FilePath,
            int OffsetX,
            int OffsetY
        );

        public sealed record MapSection(
            int OriginX,
            int OriginY,
            int Width,
            int Height,
            byte Z,
            MinimapLayer Layer,
            MapBounds Bounds,
            IReadOnlyList<MapSectionTile> Tiles
        );
        
        public sealed record MinimapMarker(
            int X,
            int Y,
            byte Z,
            byte IconId,
            string Text
        );

        public sealed record MonsterSpawnMarker(
            int X,
            int Y,
            byte Z,
            string MonsterName,
            int? CreatureId,
            int? SpawnTimeSeconds,
            byte? Direction
        );
    }
}
