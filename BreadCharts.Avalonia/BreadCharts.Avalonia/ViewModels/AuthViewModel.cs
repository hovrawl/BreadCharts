using System;
using System.Threading.Tasks;
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

    public bool IsBrowser => _authService.IsBrowser;

    public AuthViewModel(AuthService authService, NavigationService navService)
    {
        _authService = authService;
        _navService = navService;
    }

    [RelayCommand]
    public async Task OpenAuth()
    {
        var session = await GetAuthSession();
        _authService.OpenUrl(session.RedirectUri);
    }

    public async Task<AuthSession> GetAuthSession()
    {
        return await _authService.BeginAuth();
    }

    public void HandleCallback(Uri uri)
    {
        _authService.HandleCallbackAsync(uri);
    }
    
    public void SetAccessToken(string accessToken)
    {
        AccessToken = accessToken;
        //_navService.NavigateToDashboard();
    }
}