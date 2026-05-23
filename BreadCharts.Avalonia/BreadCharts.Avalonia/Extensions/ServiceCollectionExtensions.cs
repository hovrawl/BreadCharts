using System;
using System.Net.Http;
using BreadCharts.Avalonia.Services;
using BreadCharts.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BreadCharts.Avalonia.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection, string? baseAddress = null)
    {
        baseAddress ??= "https://127.0.0.1:7206";
        collection.AddSingleton<HttpClient>(new HttpClient { BaseAddress = new Uri(baseAddress) });
        collection.AddSingleton<ServerDiscoveryService>();
        collection.AddSingleton<ApiClient>();
        collection.AddSingleton<AuthService>();
        collection.AddSingleton<SpotifyService>();
        collection.AddSingleton<NavigationFactory>();
        collection.AddSingleton<NavigationService>();
        collection.AddSingleton<ImageService>();
        collection.AddTransient<MainViewModel>();
        collection.AddTransient<ServerSelectionViewModel>();
        collection.AddTransient<AuthViewModel>();
        collection.AddTransient<SearchViewModel>();
        collection.AddScoped<ChartOptionDetailsViewModel>();
    }
}