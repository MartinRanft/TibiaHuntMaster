using TibiaHuntMaster.Core.Abstractions.Map;

namespace TibiaHuntMaster.Infrastructure.Services.Map
{
    public sealed class MapSectionService(IMinimapTileCatalog catalog) : IMapSectionService
    {
        private const int TileSize = 256;

        public Core.Map.Map.MapSection GetSection(Core.Map.Map.MapSectionRequest request)
        {
            int halfW = request.Width / 2;
            int halfH = request.Height / 2;

            int originX = request.CenterX - halfW;
            int originY = request.CenterY - halfH;

            int minX = originX;
            int minY = originY;
            int maxX = originX + request.Width - 1;
            int maxY = originY + request.Height - 1;

            // Compute tile grid covering the bounds
            int startTileX = FloorToTile(minX);
            int startTileY = FloorToTile(minY);
            int endTileX = FloorToTile(maxX);
            int endTileY = FloorToTile(maxY);

            List<Core.Map.Map.MapSectionTile> tiles = new List<Core.Map.Map.MapSectionTile>(capacity: 64);

            int tileY = startTileY;
            while (tileY <= endTileY)
            {
                int tileX = startTileX;
                while (tileX <= endTileX)
                {
                    if (catalog.TryGetTilePath(request.Layer, tileX, tileY, request.Z, out string path))
                    {
                        int offsetX = tileX - originX;
                        int offsetY = tileY - originY;

                        tiles.Add(new Core.Map.Map.MapSectionTile(
                            TileAbsX: tileX,
                            TileAbsY: tileY,
                            Z: request.Z,
                            Layer: request.Layer,
                            FilePath: path,
                            OffsetX: offsetX,
                            OffsetY: offsetY
                        ));
                    }

                    tileX += TileSize;
                }

                tileY += TileSize;
            }

            Core.Map.Map.MapBounds bounds = new Core.Map.Map.MapBounds(minX, minY, maxX, maxY, request.Z);

            return new Core.Map.Map.MapSection(
                OriginX: originX,
                OriginY: originY,
                Width: request.Width,
                Height: request.Height,
                Z: request.Z,
                Layer: request.Layer,
                Bounds: bounds,
                Tiles: tiles
            );
        }

        private static int FloorToTile(int value)
        {
            // floor(value / 256) * 256
            int div = value / TileSize;
            if (value < 0 && (value % TileSize) != 0)
            {
                div -= 1;
            }

            return div * TileSize;
        }
    }
}
