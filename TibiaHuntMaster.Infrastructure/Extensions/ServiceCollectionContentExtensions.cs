using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TibiaHuntMaster.Infrastructure.Http.Content.Abstractions;
using TibiaHuntMaster.Infrastructure.Http.Content.Assets;
using TibiaHuntMaster.Infrastructure.Http.Content.Creatures;
using TibiaHuntMaster.Infrastructure.Http.Content.HuntingPlaces;
using TibiaHuntMaster.Infrastructure.Http.Content.Items;
using TibiaHuntMaster.Infrastructure.Http.Content.Shared;
using TibiaHuntMaster.Infrastructure.Services.Content;
using TibiaHuntMaster.Infrastructure.Services.Content.Imports;
using TibiaHuntMaster.Infrastructure.Services.Content.Imports.Interfaces;
using TibiaHuntMaster.Infrastructure.Services.Content.Interfaces;

namespace TibiaHuntMaster.Infrastructure.Extensions
{
    public static class ServiceCollectionContentExtensions
    {
        public static IServiceCollection AddContentInfrastructure(this IServiceCollection services)
        {
            services.Configure<ContentClientOptions>(_ => { });

            services.AddHttpClient<IItemsClient, ItemClient>(ConfigureContentHttpClient);
            services.AddHttpClient<ICreaturesClient, CreaturesClient>(ConfigureContentHttpClient);
            services.AddHttpClient<IHuntingPlacesClient, HuntingPlacesClient>(ConfigureContentHttpClient);
            services.AddHttpClient<IAssetsClient, AssetsClient>(ConfigureContentHttpClient);

            services.AddSingleton<IContentProgressService, ContentProgressService>();
            services.AddSingleton<IItemContentImportService, ItemContentImportService>();
            services.AddSingleton<ICreatureContentImportService, CreatureContentImportService>();
            services.AddSingleton<IHuntingPlaceContentImportService, HuntingPlaceContentImportService>();
            services.AddSingleton<IContentService, ContentService>();

            return services;
        }

        private static void ConfigureContentHttpClient(IServiceProvider serviceProvider, HttpClient client)
        {
            ContentClientOptions options = serviceProvider.GetRequiredService<IOptions<ContentClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        }
    }
}
