using System;
using System.Globalization;
using System.IO;

using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace TibiaHuntMaster.App.Converters
{
    internal sealed class VocationToImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is not string vocation)
            {
                return null;
            }

            string imageName = vocation.GetVocationImageName();
            if(string.IsNullOrEmpty(imageName))
            {
                return null;
            }

            try
            {
                Uri uri = new($"avares://TibiaHuntMaster.App/Assets/Vocations/{imageName}");
                using Stream stream = AssetLoader.Open(uri);
                return new Bitmap(stream);
            }
            catch
            {
                // Optional: Logging einbauen
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal static class VocationExtensions
    {
        // Instanz-Erweiterung für string
        extension(string vocation)
        {
            public string GetVocationImageName()
            {
                if(string.IsNullOrWhiteSpace(vocation))
                {
                    // Fallback (z. B. Rookgaard / Unknown)
                    return "monk.png";
                }

                // Case-insensitive Matching, ohne ToLower()
                if(vocation.Contains("knight", StringComparison.OrdinalIgnoreCase))
                {
                    return "vocations_knight.png";
                }

                if(vocation.Contains("paladin", StringComparison.OrdinalIgnoreCase))
                {
                    return "vocations_paladin.png";
                }

                if(vocation.Contains("sorcerer", StringComparison.OrdinalIgnoreCase))
                {
                    return "vocations_sorcerer.png";
                }

                if(vocation.Contains("druid", StringComparison.OrdinalIgnoreCase))
                {
                    return "vocations_druid.png";
                }

                if(vocation.Contains("monk", StringComparison.OrdinalIgnoreCase) ||
                   vocation.Contains("exalted", StringComparison.OrdinalIgnoreCase))
                {
                    return "Monk_Artwork.png";
                }

                // Default, falls nichts matched
                return "monk.png";
            }
        }
    }
}