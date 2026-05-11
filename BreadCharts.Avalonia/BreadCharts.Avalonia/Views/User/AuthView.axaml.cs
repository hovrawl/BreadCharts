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
        
        // Find the NativeWebView by name. We use dynamic/object to avoid missing type issues 
        // if the exact package version isn't known, but usually it has a Url property or similar.
        var webView = this.FindControl<Control>("AuthWebView");
        if (webView != null)
        {
            // Try to hook into navigation events if available on this specific WebView implementation
            // Since we don't have the exact type here, we use a generic approach or assume standard WebView behavior.
            // For now, we'll try to use the dynamic approach if we can't find the exact type.
        }
    }

    // This is a placeholder. In a real scenario, you'd use the specific event for the WebView control.
    private void HandleUrlChange(string? url)
    {
        if (url?.Contains("http://localhost:5543/callback") == true)
        {
            if (DataContext is AuthViewModel viewModel)
            {
                viewModel.HandleCallback(new Uri(url));
            }
        }
    }
}