using BreadCharts.Core.Infrastructure;
using BreadCharts.Core.Models;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;

namespace BreadCharts.Core.Services;

public class SpotifyAuthService : IAuthenticationService
{
    private readonly string _redirectUri;
    private readonly string _clientId;
    private readonly string _clientSecret;
    
    internal string accessToken;
    internal string refreshToken;
    internal SpotifyClient? spotify;
    
    private int pageSize = 20;

    public void SetTokens(string access, string? refresh = null)
    {
        accessToken = access;
        if (!string.IsNullOrEmpty(refresh)) refreshToken = refresh!;
        // Reconfigure client with token for immediate usage
        spotify = ConfigureSpotifyClientFromToken();
    }
    
    public bool Authed => spotify != null && !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken);

    public SpotifyAuthService(IConfiguration configRoot)
    {
        var authServiceConfig = configRoot.GetSection("AuthServiceConfig").Get<AuthServiceConfig>();
        
        _redirectUri = authServiceConfig.RedirectUri;
        _clientId = authServiceConfig.ClientId;
        _clientSecret = authServiceConfig.ClientSecret;
    }
    
    public Uri LoginChallenge()
    {
        var loginRequest = new LoginRequest(
            new Uri(_redirectUri),
            _clientId,
            LoginRequest.ResponseType.Code
        )
        {
            Scope = new[]
            {
                Scopes.Streaming, Scopes.PlaylistModifyPrivate, 
                Scopes.PlaylistModifyPublic, Scopes.UserReadPrivate,
                Scopes.UserTopRead,
            }
        };
        var uri = loginRequest.ToUri();

        return uri;
    }
    
    public async Task<bool> ChallengeCallback(string userId, string code)
    {
        var response = await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(_clientId, _clientSecret, code, new Uri(_redirectUri))
        );
        
        
        spotify = ConfigureSpotifyClientFromCode(response);
        
        
        return true;
    }

    public async Task<UserProfile> GetUserProfile()
    {
        spotify ??= GetSpotifyClient();
        if (!Authed) return null;

        var privateUser = await spotify.UserProfile.Current();

        var userProfile = new UserProfile
        {
            Id = privateUser.Id,
            Name = privateUser.DisplayName
        };
        return userProfile;
    }
    
    public async Task<List<FullTrack>> GetBasicTracks()
    {
        var returnList = new List<FullTrack>();
        spotify ??= GetSpotifyClient();
        if (spotify == null) return returnList;

        var tracksResponse = await spotify.UserProfile.GetTopTracks(new UsersTopItemsRequest(TimeRange.MediumTerm));

        if (tracksResponse == null) return returnList;
        
        return tracksResponse?.Items?.ToList();
    }
    
    public async Task<List<ChartOption>> Search(string searchTerm, int page = -1)
    {
        spotify ??= GetSpotifyClient();
        if (spotify == null) return null;
        
        var returnList = new List<ChartOption>();
        if (string.IsNullOrEmpty(searchTerm)) return returnList;
        
        var offset = page > 0 ? page * pageSize : 0;
        var searchRequest = new SearchRequest(SearchRequest.Types.All, searchTerm)
        {
            Offset = offset
        };
        
        var searchResponse = await spotify.Search.Item(searchRequest);

        if (searchResponse == null) return returnList;

      
        if (searchResponse.Artists.Items != null)
        {
            foreach (var artist in searchResponse.Artists.Items)
            {
                var chartOption = new ChartOption
                {
                    Id = artist.Id,
                    Name = artist.Name,
                    Type = ChartOptionType.Artist
                };
                returnList.Add(chartOption);
            }
        }
        
        if (searchResponse.Albums.Items != null)
        {
            foreach (var album in searchResponse.Albums.Items)
            {
                var chartOption = new ChartOption
                {
                    Id = album.Id,
                    Name = album.Name,
                    Type = ChartOptionType.Album
                };
                returnList.Add(chartOption);
            }
        }

        if (searchResponse.Tracks?.Items != null)
        {
            foreach (var track in searchResponse.Tracks.Items)
            {
                var trackName = $"{string.Join(", ", track.Artists.Select(art => art.Name))} - {track.Name}";
                var chartOption = new ChartOption
                {
                    Id = track.Id,
                    Name = trackName,
                    Type = ChartOptionType.Track
                };
                returnList.Add(chartOption);
            }
        }

        if (searchResponse.Playlists.Items != null)
        {
            foreach (var playlist in searchResponse.Playlists.Items)
            {
                if (playlist == null) continue;
                var chartOption = new ChartOption
                {
                    Id = playlist.Id,
                    Name = playlist.Name,
                    Type = ChartOptionType.Playlist
                };
                returnList.Add(chartOption);
            }
        }

        return returnList;
    }
    
    public async Task GetInfo(string id)
    {
        
        spotify ??= GetSpotifyClient();
        if (spotify == null) return;
        
        var info = await spotify.Tracks.Get(id);
        
    }

    private SpotifyClient GetSpotifyClient()
    {
        if (spotify == null && !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
        {
            
            spotify = ConfigureSpotifyClientFromToken();
        }
        
        return spotify;
    }
    
    private SpotifyClient ConfigureSpotifyClientFromCode(AuthorizationCodeTokenResponse response)
    {
        var config = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new AuthorizationCodeAuthenticator(_clientId, _clientSecret, response));
          
        accessToken = response.AccessToken;
        refreshToken = response.RefreshToken;
        
        var spotify = new SpotifyClient(config);

        return spotify;
    }
    
    private SpotifyClient ConfigureSpotifyClientFromToken()
    {
        var config = SpotifyClientConfig
            .CreateDefault().WithToken(accessToken);
        
        var spotify = new SpotifyClient(config);

        return spotify;
    }

   
}