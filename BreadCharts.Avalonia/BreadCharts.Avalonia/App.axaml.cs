using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using BreadCharts.Avalonia.Extensions;
using BreadCharts.Avalonia.Services;
using BreadCharts.Avalonia.ViewModels;
using BreadCharts.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BreadCharts.Avalonia;

public partial class App : Application
{
    public string? BaseAddress { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Register all the services needed for the application to run
        var collection = new ServiceCollection();
        collection.AddCommonServices(BaseAddress);

        // Creates a ServiceProvider containing services from the provided IServiceCollection
        var services = collection.BuildServiceProvider();

        if (BaseAddress != null)
        {
            var authService = services.GetRequiredService<AuthService>();
            authService.SetRedirectBase(BaseAddress);
            authService.SetApiBaseUrl(BaseAddress);
        }

        var vm = services.GetRequiredService<MainViewModel>();

        // Check for pending auth result (especially for WASM reload)
        _ = CheckForPendingAuth(vm);
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = vm }; 
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = vm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task CheckForPendingAuth(MainViewModel vm)
    {
        AuthService.Log("CheckForPendingAuth started");
        var authSession = await vm.AuthService.BeginAuth();
        // If BeginAuth immediately returns a completed task (via _pendingResult), this will proceed
        if (authSession.TokenTask.IsCompleted)
        {
            AuthService.Log("Found immediate auth result during startup check");
            var result = await authSession.TokenTask;
            await vm.HandleAuthResult(result);
        }
        else
        {
            AuthService.Log("No immediate auth result found during startup check");
        }
    }
}