using System;
using BreadCharts.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BreadCharts.Avalonia.ViewModels;

public partial class AuthViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _accessToken;

    private readonly AuthService _authService;
    private readonly NavigationService _navService;

    public Uri AuthUri { get; set; }
    
    public AuthViewModel(AuthService authService, NavigationService navService)
    {
        _authService = authService;
        _navService = navService;
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