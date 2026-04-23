using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

namespace TibiaHuntMaster.App.Converters
{
    public sealed class CurrentUserColorConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // Value[0] = Name in der Zeile (z.B. "Mister Pilsner")
            // Value[1] = Name des aktiven Chars (z.B. "Tentakel")

            if(values.Count == 2 &&
               values[0] is string memberName &&
               values[1] is string activeCharName)
            {
                if(memberName.Equals(activeCharName, StringComparison.OrdinalIgnoreCase))
                {
                    return SolidColorBrush.Parse("#D4AF37"); // Gold (Hervorhebung)
                }
            }

            // Standard Farbe (Weiß/Grau)
            return SolidColorBrush.Parse("#dddddd");
        }
    }
}