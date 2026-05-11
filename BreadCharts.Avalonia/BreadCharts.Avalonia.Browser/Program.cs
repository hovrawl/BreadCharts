using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using BreadCharts.Avalonia;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length > 0 && Uri.TryCreate(args[0], UriKind.Absolute, out var uri))
        {
            var authService = new BreadCharts.Avalonia.Services.AuthService();
            var result = authService.ParseResult(uri);
            if (result != null)
            {
                BreadCharts.Avalonia.Services.AuthService.SetPendingResult(result);
            }
        }

        await BuildAvaloniaApp()
            .WithInterFont()
#if DEBUG
            .WithDeveloperTools()
#endif
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}