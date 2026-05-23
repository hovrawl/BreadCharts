using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BreadCharts.Avalonia.ViewModels;

namespace BreadCharts.Avalonia.Views.User;

public partial class AuthView : UserControl
{
    public const string ViewName = "Auth";
    
    public AuthView()
    {
        InitializeComponent();
        
        if (Design.IsDesignMode) return;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsBrowser())
        {
            var webView = this.FindControl<Control>("AuthWebView");
            if (webView != null) webView.IsVisible = false;
            
            var browserPanel = this.FindControl<Control>("BrowserAuthPanel");
            if (browserPanel != null) browserPanel.IsVisible = true;
        }
        else
        {
            if (DataContext is AuthViewModel viewModel)
            {
                var session = await viewModel.GetAuthSession();
                var webView = this.FindControl<NativeWebView>("AuthWebView");
                if (webView != null)
                {
                    webView.Source = session.RedirectUri;
                }
            }
        }
    }

    // This is a placeholder. In a real scenario, you'd use the specific event for the WebView control.
    private void HandleUrlChange(Uri? url)
    {
        if (url == null) return;
        if (DataContext is AuthViewModel viewModel)
        {
            viewModel.HandleCallback(url);
        }
    }

    private void AuthWebView_OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        HandleUrlChange(e.Request);
    }
}