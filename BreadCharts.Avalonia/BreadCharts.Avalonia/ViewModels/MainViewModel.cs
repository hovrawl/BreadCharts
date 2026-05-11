using System.Threading.Tasks;
using BreadCharts.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using SpotifyAPI.Web;

namespace BreadCharts.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    
    private AuthService _authService;
    private NavigationService _navService;
    private SpotifyService _spotifyService;
    private ApiClient _apiClient;
    
    public AuthService AuthService => _authService;
    public NavigationService NavService => _navService;
    public SpotifyService SpotifyService => _spotifyService;
    public ApiClient ApiClient => _apiClient;
    
    public MainViewModel(AuthService authService, NavigationService navService, SpotifyService spotifyService, ApiClient apiClient)
    {
        _authService = authService;
        _navService = navService;
        _spotifyService = spotifyService;
        _apiClient = apiClient;
    }

    public async Task HandleAuthResult(AuthResult result)
    {
        _apiClient.SetAppToken(result.AppToken);
        var userProfile = await _authService.InitUser(result.SpotifyToken);
        CurrentUser = userProfile;
        // Navigation and other setup
    }
}
