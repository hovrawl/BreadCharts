using System;
using System.Net.Http;
using BreadCharts.Avalonia.Services;
using BreadCharts.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BreadCharts.Avalonia.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection, string? webBaseAddress = null, string? apiBaseAddress = null)
    {
        // Default API address if none provided
        apiBaseAddress ??= "https://localhost:7206";
        apiBaseAddress = apiBaseAddress.Replace("localhost", "127.0.0.1").TrimEnd('/');

        // Default Web address to API address if none provided (usual for desktop)
        webBaseAddress ??= apiBaseAddress;
        webBaseAddress = webBaseAddress.Replace("localhost", "127.0.0.1").TrimEnd('/');
        
        collection.AddSingleton<HttpClient>(new HttpClient { BaseAddress = new Uri(apiBaseAddress) });
        collection.AddSingleton<ApiClient>();
        
        collection.AddSingleton<AuthService>(sp => 
        {
            var auth = new AuthService(sp.GetRequiredService<HttpClient>());
            auth.SetApiBaseUrl(apiBaseAddress);
            auth.SetRedirectBase(webBaseAddress);
            return auth;
        });
        
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