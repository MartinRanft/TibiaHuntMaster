using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

namespace TibiaHuntMaster.App.Converters
{
    public sealed class BalanceColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is long balance)
            {
                if(balance > 0)
                {
                    return SolidColorBrush.Parse("#4caf50"); // Grün (Profit)
                }
                if(balance < 0)
                {
                    return SolidColorBrush.Parse("#ff5555"); // Rot (Waste)
                }
            }

            // Neutral (0 oder kein Wert)
            return SolidColorBrush.Parse("#aaaaaa");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is SolidColorBrush brush)
            {
                string color = brush.Color.ToString();

                if(color.Equals("#FF4CAF50", StringComparison.OrdinalIgnoreCase))
                {
                    return 1L; // Positive balance
                }
                if(color.Equals("#FFFF5555", StringComparison.OrdinalIgnoreCase))
                {
                    return -1L; // Negative balance
                }
                if(color.Equals("#FFAAAAAA", StringComparison.OrdinalIgnoreCase))
                {
                    return 0L; // Neutral
                }
            }

            return 0L;
        }
    }
}