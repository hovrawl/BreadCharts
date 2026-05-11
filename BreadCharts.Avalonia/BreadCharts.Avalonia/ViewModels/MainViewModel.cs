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
    
    public AuthService AuthService => _authService;
    public NavigationService NavService => _navService;
    public SpotifyService SpotifyService => _spotifyService;
    
    public MainViewModel(AuthService authService, NavigationService navService, SpotifyService spotifyService)
    {
        _authService = authService;
        _navService = navService;
        _spotifyService = spotifyService;
    }

    public async Task HandleUserToken(AuthorizationCodeTokenResponse token)
    {
        var userProfile = await _authService.InitUser(token);
        // Set user profile
        CurrentUser = userProfile;
        //_spotifyService.GetClient(userProfile.Id, token.AccessToken, token.RefreshToken);
    }
}
