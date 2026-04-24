using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TibiaHuntMaster.App.Services.Localization;

namespace TibiaHuntMaster.App.Extensions
{
    /// <summary>
    ///     XAML markup extension for localized strings.
    ///     Usage: Text="{loc:Localize Common_Save}"
    /// </summary>
    public sealed class LocalizeExtension : MarkupExtension
    {
        public LocalizeExtension(string key)
        {
            Key = key;
        }

        public string Key { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // Get the localization service from the App's DI container
            ILocalizationService? localizationService = (Application.Current as App)?.Services?.GetService<ILocalizationService>();

            if (localizationService == null)
            {
                return $"[{Key}]";
            }

            // Create a proper binding using Avalonia's Binding class
            // The indexer binding syntax with property change notifications
            Avalonia.Data.Binding binding = new Avalonia.Data.Binding
            {
                Source = localizationService,
                Path = $"[{Key}]",
                Mode = BindingMode.OneWay
            };

            return binding;
        }
    }
}
