using System.Globalization;

using Avalonia.Data;
using Avalonia.Data.Converters;

namespace TibiaHuntMaster.App.Converters
{
    public sealed class NullableIntConverter : IValueConverter
    {
        // Supports both int and long VM properties for numeric text bindings.
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                int i => i.ToString(CultureInfo.InvariantCulture),
                long l => l.ToString(CultureInfo.InvariantCulture),
                _ => string.Empty
            };
        }

        // Parses text back to int/long depending on target type.
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is not string s)
            {
                return null;
            }

            // Leerer String -> null
            if(string.IsNullOrWhiteSpace(s))
            {
                return null;
            }

            if(!long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
            {
                return new BindingNotification(new Exception("Must be a number"), BindingErrorType.Error);
            }

            Type nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if(nonNullableType == typeof(long))
            {
                return parsed;
            }

            if(nonNullableType == typeof(int))
            {
                if(parsed is < int.MinValue or > int.MaxValue)
                {
                    return new BindingNotification(new Exception("Number is out of range"), BindingErrorType.Error);
                }

                return (int)parsed;
            }

            return parsed;
        }
    }
}
