using System;
using Avalonia;

namespace BreadCharts.Avalonia.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        string? baseAddress = null;
        if (args.Length > 0 && Uri.TryCreate(args[0], UriKind.Absolute, out var uri))
        {
            baseAddress = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}";
        }

        BuildAvaloniaApp(baseAddress)
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(string? baseAddress = null)
        => AppBuilder.Configure<App>()
            .AfterSetup(_ =>
            {
                if (App.Current is App app)
                {
                    app.BaseAddress = baseAddress;
                }
            })
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
