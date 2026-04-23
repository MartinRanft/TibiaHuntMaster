using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TibiaHuntMaster.App.Services.Map
{
    public sealed record HuntingPlaceCoordinate(int X, int Y, byte Z)
    {
        public string Display => TibiaCoordinateConverter.FormatExternalCoordinates(X, Y, Z);
    }

    public sealed record HuntingPlaceLocationParseResult(string CleanedLocation, IReadOnlyList<HuntingPlaceCoordinate> Coordinates);

    public static partial class HuntingPlaceLocationParser
    {
        [GeneratedRegex(@"\{\{\s*Mapper\s+Coords\|(?<content>[^}]*)\}\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MapperCoordsRegex();

        [GeneratedRegex(@"coords=(?<x>\d{2,3}\.\d{1,3})[,-](?<y>\d{2,3}\.\d{1,3})[,-](?<z>\d{1,2})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex LegacyCoordsRegex();

        [GeneratedRegex(@"\[\[(?<target>[^\]|]+)\|(?<label>[^\]]+)\]\]", RegexOptions.CultureInvariant)]
        private static partial Regex WikiLinkWithLabelRegex();

        [GeneratedRegex(@"\[\[(?<label>[^\]]+)\]\]", RegexOptions.CultureInvariant)]
        private static partial Regex WikiLinkPlainRegex();

        [GeneratedRegex(@"\[(https?:\/\/[^\s\]]+)\s+(?<label>[^\]]+)\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ExternalLinkWithLabelRegex();

        [GeneratedRegex(@"https?:\/\/\S+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex RawUrlRegex();

        [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex HtmlBreakRegex();

        [GeneratedRegex(@"\s{2,}", RegexOptions.CultureInvariant)]
        private static partial Regex MultiWhitespaceRegex();

        [GeneratedRegex(@"\s+([,.;:])", RegexOptions.CultureInvariant)]
        private static partial Regex SpaceBeforePunctuationRegex();

        [GeneratedRegex(@"([,.;:])\1+", RegexOptions.CultureInvariant)]
        private static partial Regex DuplicatePunctuationRegex();

        public static HuntingPlaceLocationParseResult Parse(string? rawLocation)
        {
            string original = rawLocation?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(original))
            {
                return new HuntingPlaceLocationParseResult(string.Empty, []);
            }

            List<HuntingPlaceCoordinate> coordinates = ParseCoordinates(original);
            string cleaned = CleanLocationText(original);

            return new HuntingPlaceLocationParseResult(cleaned, coordinates);
        }

        private static List<HuntingPlaceCoordinate> ParseCoordinates(string text)
        {
            List<HuntingPlaceCoordinate> result = [];
            HashSet<(int X, int Y, byte Z)> seen = [];

            foreach (Match match in MapperCoordsRegex().Matches(text))
            {
                if (!match.Success)
                {
                    continue;
                }

                if (TryParseMapperContent(match.Groups["content"].Value, out HuntingPlaceCoordinate coordinate) &&
                    seen.Add((coordinate.X, coordinate.Y, coordinate.Z)))
                {
                    result.Add(coordinate);
                }
            }

            foreach (Match match in LegacyCoordsRegex().Matches(text))
            {
                if (!match.Success)
                {
                    continue;
                }

                string xToken = match.Groups["x"].Value;
                string yToken = match.Groups["y"].Value;
                string zToken = match.Groups["z"].Value;

                if (!TibiaCoordinateConverter.TryParseExternalCoordinate(xToken, out int x))
                {
                    continue;
                }

                if (!TibiaCoordinateConverter.TryParseExternalCoordinate(yToken, out int y))
                {
                    continue;
                }

                if (!byte.TryParse(zToken, out byte z) || z > 15)
                {
                    continue;
                }

                if (seen.Add((x, y, z)))
                {
                    result.Add(new HuntingPlaceCoordinate(x, y, z));
                }
            }

            return result;
        }

        private static bool TryParseMapperContent(string content, out HuntingPlaceCoordinate coordinate)
        {
            coordinate = null!;
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            string[] tokens = content.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int? x = null;
            int? y = null;
            byte? z = null;

            foreach (string token in tokens)
            {
                if (z.HasValue)
                {
                    break;
                }

                if (!x.HasValue)
                {
                    if (TibiaCoordinateConverter.TryParseExternalCoordinate(token, out int parsedX))
                    {
                        x = parsedX;
                    }

                    continue;
                }

                if (!y.HasValue)
                {
                    if (TibiaCoordinateConverter.TryParseExternalCoordinate(token, out int parsedY))
                    {
                        y = parsedY;
                    }

                    continue;
                }

                if (byte.TryParse(token, out byte parsedZ) && parsedZ <= 15)
                {
                    z = parsedZ;
                }
            }

            if (!x.HasValue || !y.HasValue || !z.HasValue)
            {
                return false;
            }

            coordinate = new HuntingPlaceCoordinate(x.Value, y.Value, z.Value);
            return true;
        }

        private static string CleanLocationText(string text)
        {
            string cleaned = MapperCoordsRegex().Replace(text, string.Empty);
            cleaned = ExternalLinkWithLabelRegex().Replace(cleaned, "${label}");
            cleaned = RawUrlRegex().Replace(cleaned, string.Empty);
            cleaned = WikiLinkWithLabelRegex().Replace(cleaned, "${label}");
            cleaned = WikiLinkPlainRegex().Replace(cleaned, "${label}");
            cleaned = HtmlBreakRegex().Replace(cleaned, ", ");
            cleaned = cleaned.Replace("{{", string.Empty)
                             .Replace("}}", string.Empty)
                             .Replace("[", string.Empty)
                             .Replace("]", string.Empty);

            cleaned = SpaceBeforePunctuationRegex().Replace(cleaned, "$1");
            cleaned = DuplicatePunctuationRegex().Replace(cleaned, "$1");
            cleaned = MultiWhitespaceRegex().Replace(cleaned, " ");

            cleaned = cleaned.Trim();
            cleaned = cleaned.Trim(',', ';', '.', ':');

            return cleaned;
        }
    }
}
