using System.Globalization;

using TibiaHuntMaster.Core.Abstractions.Map;


namespace TibiaHuntMaster.Infrastructure.Services.Map
{
    public class MinimapTileCatalog : IMinimapTileCatalog
    {
        private readonly Dictionary<TileKey, string> _paths;
        private readonly Dictionary<byte, Core.Map.Map.MapBounds> _boundsByZ;
        private readonly record struct TileKey(Core.Map.Map.MinimapLayer Layer, int AbsX, int AbsY, byte Z);

        public MinimapTileCatalog(string mapDirectory)
        {
            if(!Directory.Exists(mapDirectory))
            {
                throw new DirectoryNotFoundException($"Map directory not found: {mapDirectory}");
            }
            _paths = new Dictionary<TileKey, string>(capacity: 100000);
            _boundsByZ = new Dictionary<byte, Core.Map.Map.MapBounds>(capacity: 16);
        }

        public bool TryGetTilePath(Core.Map.Map.MinimapLayer layer, int absX, int absY, byte z, out string filePath)
        {
            TileKey key = new TileKey(layer, absX, absY, z);
            if(_paths.TryGetValue(key, out string? resolvedPath))
            {
                filePath = resolvedPath;
                return true;
            }

            filePath = string.Empty;
            return false;
        }

        public Core.Map.Map.MapBounds GetKnownBounds(byte z)
        {
            if(_boundsByZ.TryGetValue(z, out Core.Map.Map.MapBounds bounds))
            {
                return bounds;
            }

            return new Core.Map.Map.MapBounds(0, 0, 0, 0, z);
        }
        
        private void IndexDirectory(string mapDirectory)
        {
            IEnumerable<string> files = Directory.EnumerateFiles(mapDirectory, "*.png", SearchOption.TopDirectoryOnly);

            foreach (string fullPath in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(fullPath);
                if (!TryParseFileName(fileName, out Core.Map.Map.MinimapLayer layer, out int absX, out int absY, out byte z))
                {
                    continue;
                }

                TileKey key = new TileKey(layer, absX, absY, z);
                _paths[key] = fullPath;

                UpdateBounds(z, absX, absY);
            }
        }
        
        private void UpdateBounds(byte z, int absX, int absY)
        {
            // Tile is 256×256, absX/absY = top-left of that tile. :contentReference[oaicite:4]{index=4}
            const int tileSize = 256;
            int maxX = absX + tileSize - 1;
            int maxY = absY + tileSize - 1;

            if (!_boundsByZ.TryGetValue(z, out Core.Map.Map.MapBounds b))
            {
                _boundsByZ[z] = new Core.Map.Map.MapBounds(absX, absY, maxX, maxY, z);
                return;
            }

            int minX2 = (absX < b.MinX) ? absX : b.MinX;
            int minY2 = (absY < b.MinY) ? absY : b.MinY;
            int maxX2 = (maxX > b.MaxX) ? maxX : b.MaxX;
            int maxY2 = (maxY > b.MaxY) ? maxY : b.MaxY;

            _boundsByZ[z] = new Core.Map.Map.MapBounds(minX2, minY2, maxX2, maxY2, z);
        }
        
        private static bool TryParseFileName(
            string fileName,
            out Core.Map.Map.MinimapLayer layer,
            out int absX,
            out int absY,
            out byte z)
        {
            layer = Core.Map.Map.MinimapLayer.Color;
            absX = 0;
            absY = 0;
            z = 0;

            // Expected:
            // Minimap_Color_31744_30976_10
            // Minimap_WaypointCost_32000_32256_13
            string[] parts = fileName.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
            {
                return false;
            }

            if (!string.Equals(parts[0], "Minimap", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(parts[1], "Color", StringComparison.OrdinalIgnoreCase))
            {
                layer = Core.Map.Map.MinimapLayer.Color;
            }
            else if (string.Equals(parts[1], "WaypointCost", StringComparison.OrdinalIgnoreCase))
            {
                layer = Core.Map.Map.MinimapLayer.WaypointCost;
            }
            else
            {
                return false;
            }

            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out absX))
            {
                return false;
            }

            if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out absY))
            {
                return false;
            }

            if (!byte.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out z))
            {
                return false;
            }

            return true;
        }
    }
}
