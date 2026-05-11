using System;
using Avalonia;
using Avalonia.Controls;
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

        // Use a slight delay to ensure DataContext is set if needed, or just check platform
        if (OperatingSystem.IsBrowser())
        {
            var webView = this.FindControl<Control>("AuthWebView");
            if (webView != null) webView.IsVisible = false;
            
            var browserPanel = this.FindControl<Control>("BrowserAuthPanel");
            if (browserPanel != null) browserPanel.IsVisible = true;
        }
    }

    // This is a placeholder. In a real scenario, you'd use the specific event for the WebView control.
    private void HandleUrlChange(string? url)
    {
        if (url?.Contains("http://127.0.0.1:5543/callback") == true)
        {
            if (DataContext is AuthViewModel viewModel)
            {
                viewModel.HandleCallback(new Uri(url));
            }
        }
    }
}