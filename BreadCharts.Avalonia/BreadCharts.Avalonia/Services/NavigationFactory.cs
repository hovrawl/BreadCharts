using System;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace BreadCharts.Avalonia.Services;

public class NavigationFactory : IFANavigationPageFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public NavigationFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    // Create a page based on a Type, but you can create it however you want
    public Control GetPage(Type srcType)
    {
        // Resolve the View from the DI container if it's registered, otherwise instantiate it
        var page = _serviceProvider.GetService(srcType) as Control ?? 
                   Activator.CreateInstance(srcType) as Control;

        if (page == null)
            return null;

        // Convention: View name -> ViewModel name (e.g., AuthView -> AuthViewModel)
        // If the view is HomeView, it seems to use MainViewModel based on x:DataType in HomeView.axaml
        var viewModelName = srcType.Name.Replace("View", "ViewModel");
        var viewModelNamespace = "BreadCharts.Avalonia.ViewModels";
        
        // Handle special case for HomeView -> MainViewModel
        if (srcType.Name == "HomeView")
        {
            viewModelName = "MainViewModel";
        }

        var viewModelType = Type.GetType($"{viewModelNamespace}.{viewModelName}, {srcType.Assembly.FullName}");

        if (viewModelType != null)
        {
            var viewModel = _serviceProvider.GetService(viewModelType);
            if (viewModel != null)
            {
                page.DataContext = viewModel;
            }
        }
        
        return page;
    }

    // Create a page based on an object, such as a view model
    public Control GetPageFromObject(object target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        // Convention: ViewModel name -> View name (e.g., AuthViewModel -> AuthView)
        // Special case: MainViewModel -> HomeView (as HomeView uses MainViewModel)
        var viewName = target.GetType().Name.Replace("ViewModel", "View");
        var viewNamespace = "BreadCharts.Avalonia.Views";

        if (target.GetType().Name == "MainViewModel")
        {
            viewName = "HomeView";
        }
        
        // Try to find the view type in multiple possible namespaces
        var viewType = Type.GetType($"{viewNamespace}.{viewName}, {target.GetType().Assembly.FullName}") ??
                       Type.GetType($"{viewNamespace}.User.{viewName}, {target.GetType().Assembly.FullName}");

        if (viewType != null)
        {
            var page = _serviceProvider.GetService(viewType) as Control ?? 
                       Activator.CreateInstance(viewType) as Control;

            if (page != null)
            {
                page.DataContext = target;
                return page;
            }
        }

        throw new Exception($"Could not find view for {target.GetType().Name}");
    }
}