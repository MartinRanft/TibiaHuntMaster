using System;
using System.Globalization;

namespace TibiaHuntMaster.App.Services.Map
{
    public static class TibiaCoordinateConverter
    {
        public const int TileSize = 256;

        public static bool TryParseExternalCoordinate(string token, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string trimmed = token.Trim();
            string[] parts = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int major))
            {
                return false;
            }

            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minor))
            {
                return false;
            }

            if (major <= 0 || minor < 0 || minor >= TileSize)
            {
                return false;
            }

            value = (major * TileSize) + minor;
            return true;
        }

        public static string FormatExternalCoordinate(int value)
        {
            int major = Math.DivRem(value, TileSize, out int minor);
            if (minor < 0)
            {
                major -= 1;
                minor += TileSize;
            }

            return $"{major}.{minor}";
        }

        public static string FormatExternalCoordinates(int x, int y, byte z)
        {
            return $"{FormatExternalCoordinate(x)},{FormatExternalCoordinate(y)},{z}";
        }
    }
}
