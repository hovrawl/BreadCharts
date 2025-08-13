using SpotifyAPI.Web;

namespace BreadCharts.Core.Services;

public class SpotifyAuthService
{
    private readonly string _redirectUri;
    private readonly string _clientId;
    private readonly string _clientSecret;
    
    internal string accessToken;
    internal string refreshToken;
    internal SpotifyClient spotify;
    
    public bool Authed => spotify != null && !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken);

    public SpotifyAuthService(string redirectUri, string clientId, string clientSecret)
    {
        _redirectUri = redirectUri;
        _clientId = clientId;
        _clientSecret = clientSecret;
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
    
    public async Task<bool> GetCallback(string code)
    {
        var response = await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(_clientId, _clientSecret, code, new Uri(_redirectUri))
        );
        
        
        spotify = GetSpotifyClient(response);
        
        
        return true;
    }

    public async Task<PrivateUser> GetUserProfile()
    {
        if (!Authed) return null;

        var tracksResponse = await spotify.UserProfile.Current();

        return tracksResponse;
    }
    
    public async Task<List<FullTrack>> GetBasicTracks()
    {
        var returnList = new List<FullTrack>();
        if (!Authed) return returnList;

        var tracksResponse = await spotify.UserProfile.GetTopTracks(new UsersTopItemsRequest(TimeRange.MediumTerm));

        if (tracksResponse == null) return returnList;
        
        return tracksResponse?.Items?.ToList();
    }

    private SpotifyClient GetSpotifyClient(AuthorizationCodeTokenResponse response)
    {
        var config = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new AuthorizationCodeAuthenticator(_clientId, _clientSecret, response));
          
        accessToken = response.AccessToken;
        refreshToken = response.RefreshToken;
        
        var spotify = new SpotifyClient(config);

        return spotify;
    }
}