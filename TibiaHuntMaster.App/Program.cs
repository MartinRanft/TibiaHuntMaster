using Avalonia;

namespace TibiaHuntMaster.App
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                             .UsePlatformDetect()
                             .With(new X11PlatformOptions
                             {
                                 // Avoid Linux shutdown crashes in some desktop environments
                                 // where DBus menu exporter callbacks race with dispatcher teardown.
                                 UseDBusMenu = false
                             })
                             .WithInterFont()
                             .LogToTrace();
        }
    }
}
