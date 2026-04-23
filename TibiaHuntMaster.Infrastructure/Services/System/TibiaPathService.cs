using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;

namespace TibiaHuntMaster.Infrastructure.Services.System
{
    public sealed class TibiaPathService(ILogger<TibiaPathService> logger)
    {
        public string? GetEventSchedulePath()
        {
            string basePath = "";

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: %LOCALAPPDATA%\Tibia\packages\Tibia\cache\eventschedule.json
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                basePath = Path.Combine(localAppData, "Tibia", "packages", "Tibia", "cache");
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: ~/.local/share/CipSoft GmbH/Tibia/packages/Tibia/cache/eventschedule.json
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); // /home/martin
                basePath = Path.Combine(home, ".local", "share", "CipSoft GmbH", "Tibia", "packages", "Tibia", "cache");
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Mac (Vermutung, müsste geprüft werden, aber Fallback ist sicher)
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                basePath = Path.Combine(home, "Library", "Application Support", "CipSoft GmbH", "Tibia", "packages", "Tibia", "cache");
            }

            string fullPath = Path.Combine(basePath, "eventschedule.json");

            if(File.Exists(fullPath))
            {
                logger.LogInformation("Found local Tibia event schedule at: {Path}", fullPath);
                return fullPath;
            }

            logger.LogWarning("Could not find Tibia event schedule at: {Path}", fullPath);
            return null;
        }
    }
}