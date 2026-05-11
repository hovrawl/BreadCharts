using System;
using BreadCharts.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BreadCharts.Avalonia.ViewModels;

public partial class AuthViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _accessToken;

    private readonly NavigationService _navService;

    public Uri AuthUri { get; set; }
    
    public AuthViewModel(NavigationService navService)
    {
        _navService = navService;
    }
    
    public void SetAccessToken(string accessToken)
    {
        _accessToken = accessToken;
        //_navService.NavigateToDashboard();
    }
}