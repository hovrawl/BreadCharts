using System;
using System.Threading.Tasks;
using BreadCharts.Avalonia.ViewModels;
using BreadCharts.Avalonia.Views;
using BreadCharts.Avalonia.Views.User;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;

namespace BreadCharts.Avalonia.Services;

public class NavigationService
{
    private readonly NavigationFactory _navigationFactory;
    private readonly IServiceProvider _serviceProvider;
    private FAFrame _frame;
    
    public NavigationService(NavigationFactory navigationFactory, IServiceProvider serviceProvider)
    {
        _navigationFactory = navigationFactory;
        _serviceProvider = serviceProvider;
    }
    
    public void SetFrame(FAFrame frame)
    {
        _frame = frame;
        _frame.NavigationPageFactory = _navigationFactory;
        _frame.Navigated += FrameOnNavigated;
    }

    private void FrameOnNavigated(object sender, FANavigationEventArgs e)
    {

        if (e.SourcePageType == typeof(AuthView))
        {
            if (e.Parameter is string uri)
            {
                if (e.Content is AuthView authView && authView.AuthWebView is not null)
                {
                    authView.AuthWebView.Source = new Uri(uri);
                }
            }
        }
    }

    public async Task NavigateToWebUri(Uri uri)
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("Frame not set. Call SetFrame before navigating.");
        }
        
        
        Navigate(AuthView.ViewName, uri.ToString());
    }

    public void Navigate(string tag, string? data = "")
    {
        if (_frame == null) return;
        if (string.IsNullOrEmpty(tag)) return;
        
        switch (tag)
        {
            case HomeView.ViewName:
            {
                _frame.Navigate(typeof(HomeView), data);
                break;
            }
            case SearchView.ViewName:
            {
                _frame.Navigate(typeof(SearchView), data);
                break;
            }
            case ProfileView.ViewName:
            {
                _frame.Navigate(typeof(ProfileView), data);
                break;
            }
            case AuthView.ViewName:
            {
                _frame.Navigate(typeof(AuthView), data);
                break;
            }
            default:
                break;
        }
    }
}