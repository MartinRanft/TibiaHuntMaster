using System.Reflection;
using TibiaHuntMaster.Updater.Core.Abstractions;

namespace TibiaHuntMaster.Updater.Core.Services.Versioning
{
    public sealed class AppVersionProvider : IAppVersionProvider
    {
        public string GetCurrentVersion()
        {
            return Assembly.GetEntryAssembly()                                                                                                                                                  
                           ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                           ?.InformationalVersion                                                                                                                                                          
                   ?? "0.0.0";   
        }
    }
}