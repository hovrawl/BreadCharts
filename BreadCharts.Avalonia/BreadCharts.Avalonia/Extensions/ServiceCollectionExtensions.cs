using BreadCharts.Avalonia.Services;
using BreadCharts.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BreadCharts.Avalonia.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddSingleton<AuthService>();
        collection.AddSingleton<SpotifyService>();
        collection.AddSingleton<NavigationFactory>();
        collection.AddSingleton<NavigationService>();
        collection.AddTransient<MainViewModel>();
        collection.AddTransient<AuthViewModel>();
    }
}