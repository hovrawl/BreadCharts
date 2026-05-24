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
        baseAddress ??= "https://localhost:7206";
        baseAddress = baseAddress.Replace("localhost", "127.0.0.1");
        
        collection.AddSingleton<HttpClient>(new HttpClient { BaseAddress = new Uri(baseAddress) });
        collection.AddSingleton<ApiClient>();
        
        var authService = new AuthService();
        authService.SetApiBaseUrl(baseAddress);
        authService.SetRedirectBase(baseAddress);
        collection.AddSingleton<AuthService>(authService);
        
        collection.AddSingleton<SpotifyService>();
        collection.AddSingleton<NavigationFactory>();
        collection.AddSingleton<NavigationService>();
        collection.AddSingleton<ImageService>();
        collection.AddTransient<MainViewModel>();
        collection.AddTransient<AuthViewModel>();
        collection.AddTransient<SearchViewModel>();
        collection.AddScoped<ChartOptionDetailsViewModel>();
    }
}