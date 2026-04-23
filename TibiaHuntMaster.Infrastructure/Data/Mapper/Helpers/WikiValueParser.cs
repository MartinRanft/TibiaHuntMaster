using System.Globalization;
using System.Text.RegularExpressions;

namespace TibiaHuntMaster.Infrastructure.Data.Mapper.Helpers
{
    public static class WikiValueParser
    {
        public static (decimal? factor, string? raw) ParsePercentToFactor(string? s)
        {
            if(string.IsNullOrWhiteSpace(s))
            {
                return (null, null);
            }
            string raw = s.Trim();
            if(raw == "--" || raw == "?" || raw.Equals("n/a", StringComparison.OrdinalIgnoreCase))
            {
                return (null, raw);
            }

            Match m = Regex.Match(raw, @"-?\d+");
            if(!m.Success)
            {
                return (null, raw);
            }

            if(decimal.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal pct))
            {
                return (Math.Clamp(pct / 100m, -10m, 10m), raw); // konservative Grenzen
            }

            return (null, raw);
        }

        public static bool? ParseYesNo(string? s)
        {
            if(string.IsNullOrWhiteSpace(s))
            {
                return null;
            }
            return s.Trim().ToLowerInvariant() switch
            {
                "yes" or "true" => true,
                "no" or "false" => false,
                _ => null
            };
        }

        public static int? ParseInt(string? s)
        {
            if(string.IsNullOrWhiteSpace(s))
            {
                return null;
            }
            s = s.Trim();
            if(s is "--" or "?")
            {
                return null;
            }
            if(int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                return v;
            }
            return null;
        }

        public static (int? min, int? max, string raw) ParseRange(string? s)
        {
            if(string.IsNullOrWhiteSpace(s))
            {
                return (null, null, "");
            }
            string raw = s.Trim();
            string[] parts = raw.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length == 2 && int.TryParse(parts[0], out int a) && int.TryParse(parts[1], out int b))
            {
                return (a, b, raw);
            }
            if(int.TryParse(raw, out int single))
            {
                return (single, single, raw);
            }
            return (null, null, raw);
        }

        public static double? ParseDouble(string? s)
        {
            if(string.IsNullOrWhiteSpace(s))
            {
                return null;
            }
            if(double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                return d;
            }
            return null;
        }
    }
}