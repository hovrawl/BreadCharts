using System;
using System.Net.Http;
using BreadCharts.Avalonia.Services;
using BreadCharts.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BreadCharts.Avalonia.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddSingleton<HttpClient>(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5206") });
        collection.AddSingleton<ApiClient>();
        collection.AddSingleton<AuthService>();
        collection.AddSingleton<SpotifyService>();
        collection.AddSingleton<NavigationFactory>();
        collection.AddSingleton<NavigationService>();
        collection.AddTransient<MainViewModel>();
        collection.AddTransient<AuthViewModel>();
    }
}