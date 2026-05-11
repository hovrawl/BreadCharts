using System;
using BreadCharts.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BreadCharts.Avalonia.ViewModels;

public partial class AuthViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _accessToken;

    private readonly AuthService _authService;
    private readonly NavigationService _navService;

    public Uri AuthUri { get; set; }

    public bool IsBrowser => _authService.IsBrowser;

    public AuthViewModel(AuthService authService, NavigationService navService)
    {
        _authService = authService;
        _navService = navService;
    }

    [RelayCommand]
    public void OpenAuth()
    {
        if (AuthUri == null) return;
        
        // Use platform-specific way to open URL
        // In WASM/Browser, it's better to use top-level redirect if popup blocked, 
        // but for now we'll assume the browser handled it or we use Native methods.
        // Actually, we can just use Process.Start or Avalonia's Launcher if available.
        // But for WASM we'll need JSHost or just let the button be a hyperlink if possible.
        // For now, let's use the NavigationService or AuthService to open it.
        _authService.OpenUrl(AuthUri);
    }

    public void HandleCallback(Uri uri)
    {
        _authService.HandleCallback(uri);
    }
    
    public void SetAccessToken(string accessToken)
    {
        _accessToken = accessToken;
        //_navService.NavigateToDashboard();
    }
}