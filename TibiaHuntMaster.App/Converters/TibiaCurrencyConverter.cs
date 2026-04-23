using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace TibiaHuntMaster.App.Converters
{
    public sealed class TibiaCurrencyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is long num)
            {
                // Negative Werte behandeln (Minus davor, dann Logik normal)
                string prefix = num < 0 ? "-" : "";
                double abs = Math.Abs((double)num);

                if(abs >= 1_000_000_000)
                {
                    return $"{prefix}{abs / 1_000_000_000.0:0.##}kkk";
                }

                if(abs >= 1_000_000)
                {
                    return $"{prefix}{abs / 1_000_000.0:0.##}kk";
                }

                if(abs >= 1_000)
                {
                    return $"{prefix}{abs / 1_000.0:0.#}k";
                }

                return num.ToString("N0"); // Unter 1k einfach die Zahl
            }
            return "0";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is not string text || string.IsNullOrWhiteSpace(text))
            {
                return 0L;
            }

            text = text.Trim().Replace(",", "").Replace(".", "");

            // Vorzeichen extrahieren
            bool isNegative = text.StartsWith("-");
            if(isNegative)
            {
                text = text.Substring(1);
            }

            // Suffix erkennen und Multiplikator anwenden
            double multiplier = 1;
            if(text.EndsWith("kkk", StringComparison.OrdinalIgnoreCase))
            {
                multiplier = 1_000_000_000;
                text = text.Substring(0, text.Length - 3);
            }
            else if(text.EndsWith("kk", StringComparison.OrdinalIgnoreCase))
            {
                multiplier = 1_000_000;
                text = text.Substring(0, text.Length - 2);
            }
            else if(text.EndsWith("k", StringComparison.OrdinalIgnoreCase))
            {
                multiplier = 1_000;
                text = text.Substring(0, text.Length - 1);
            }

            // Zahl parsen
            if(double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
            {
                long result = (long)(number * multiplier);
                return isNegative ? -result : result;
            }

            return 0L;
        }
    }
}