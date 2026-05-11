using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BreadCharts.Core.Models;
using SpotifyAPI.Web;

namespace BreadCharts.Avalonia.Services;

public class AuthService
{
    private const string ApiBaseUrl = "http://localhost:5206"; // TODO: Make dynamic
    private const string RedirectUri = "http://localhost:5543/callback";

    private TaskCompletionSource<AuthResult>? _tcs;

    public AuthService()
    {
    }

    public Task<AuthSession> BeginAuth()
    {
        _tcs = new TaskCompletionSource<AuthResult>();

        var authUrl = $"{ApiBaseUrl}/auth/spotify?redirectUrl={Uri.EscapeDataString(RedirectUri)}";

        return Task.FromResult(new AuthSession
        {
            RedirectUri = new Uri(authUrl),
            TokenTask = _tcs.Task
        });
    }

    public void HandleCallback(Uri uri)
    {
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var appToken = query["appToken"];
        var spotifyAccessToken = query["spotifyAccessToken"];
        var spotifyRefreshToken = query["spotifyRefreshToken"];
        var expiresInStr = query["expiresIn"];

        if (!string.IsNullOrEmpty(appToken) && !string.IsNullOrEmpty(spotifyAccessToken))
        {
            int.TryParse(expiresInStr, out var expiresIn);
            _tcs?.TrySetResult(new AuthResult
            {
                AppToken = appToken,
                SpotifyToken = new AuthorizationCodeTokenResponse
                {
                    AccessToken = spotifyAccessToken,
                    RefreshToken = spotifyRefreshToken,
                    ExpiresIn = expiresIn,
                    TokenType = "Bearer"
                }
            });
        }
        else
        {
            _tcs?.TrySetException(new Exception("Auth failed: Missing tokens in callback"));
        }
    }

    public async Task<UserProfile> InitUser(AuthorizationCodeTokenResponse tokenResponse)
    {
        if (tokenResponse == null)
            throw new ArgumentNullException(nameof(tokenResponse), "Token response cannot be null");
        var spotifyClient = new SpotifyClient(tokenResponse.AccessToken);
        var userProfile = await spotifyClient.UserProfile.Current();
        if (userProfile == null)
            throw new InvalidOperationException("Failed to retrieve user profile from Spotify API");

        var returnProfile = new UserProfile()
        {
            Id = userProfile.Id,
            Name = userProfile.DisplayName,
        };
        return returnProfile;
    }
}

public class AuthResult
{
    public string AppToken { get; set; } = "";
    public AuthorizationCodeTokenResponse SpotifyToken { get; set; } = null!;
}

public class AuthSession
{
    public Uri RedirectUri { get; init; }
    public Task<AuthResult> TokenTask { get; init; }
}