using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BreadCharts.Core.Models;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace BreadCharts.Avalonia.Services;

public class AuthService
{
    private static EmbedIOAuthServer _server;

    private const string _clientId = "7412007f28e045bf90c021c079e92655";
    private const string _clientSecret = "29b610aae47944668afc79084a856f0b";
    private const string _redirectUri = "http://127.0.0.1:5543/callback";

    private TaskCompletionSource<AuthorizationCodeTokenResponse>? _tcs;

    public AuthService()
    {
        // Make sure _redirectUri is in your spotify application as redirect uri!
        _server = new EmbedIOAuthServer(new Uri(_redirectUri), 5543);
    }

    public async Task<AuthSession> BeginAuth()
    {
        // Reset or initialize the TaskCompletionSource
        _tcs = new TaskCompletionSource<AuthorizationCodeTokenResponse>();

        await _server.Start();

        // Attach events
        _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
        _server.ErrorReceived += OnErrorReceived;

        var request = new LoginRequest(_server.BaseUri, _clientId, LoginRequest.ResponseType.Code)
        {
            Scope = new List<string> { Scopes.UserReadEmail }
        };

        // Return uri to open in a web view
        // BrowserUtil.Open();

        return new AuthSession
        {
            RedirectUri = request.ToUri(),
            TokenTask = _tcs.Task
        };
    }

    private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
    {
        await _server.Stop();

        var config = SpotifyClientConfig.CreateDefault();
        var tokenResponse = await new OAuthClient(config).RequestToken(
            new AuthorizationCodeTokenRequest(
                _clientId, _clientSecret, response.Code, new Uri(_redirectUri)
            )
        );

        // Detach event
        _server.AuthorizationCodeReceived -= OnAuthorizationCodeReceived;

        _tcs?.TrySetResult(tokenResponse);
    }


    private async Task OnErrorReceived(object sender, string error, string? state)
    {
        await _server.Stop();
        _server.ErrorReceived -= OnErrorReceived;
        _tcs?.TrySetException(new Exception($"Auth failed: {error}"));
    }

    /// <summary>
    /// Initialize user profile by getting user profile from Spotify API
    /// </summary>
    /// <param name="tokenResponse"></param>
    /// <returns></returns>
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

public class AuthSession
{
    public Uri RedirectUri { get; init; }
    public Task<AuthorizationCodeTokenResponse> TokenTask { get; init; }
}