using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BreadCharts.Core.Models;
using SpotifyAPI.Web;

namespace BreadCharts.Avalonia.Services;

public class AuthService
{
    private const string ApiBaseUrl = "http://127.0.0.1:5206"; // TODO: Make dynamic
    private const string RedirectUri = "http://127.0.0.1:5543/auth/callback";

    private static AuthResult? _pendingResult;
    private TaskCompletionSource<AuthResult>? _tcs;

    public bool IsBrowser => OperatingSystem.IsBrowser();

    public AuthService()
    {
    }

    public static void SetPendingResult(AuthResult result)
    {
        _pendingResult = result;
    }

    public Task<AuthSession> BeginAuth()
    {
        _tcs = new TaskCompletionSource<AuthResult>();

        if (_pendingResult != null)
        {
            var result = _pendingResult;
            _pendingResult = null;
            _tcs.SetResult(result);
        }

        var authUrl = $"{ApiBaseUrl}/auth/spotify?redirectUrl={Uri.EscapeDataString(RedirectUri)}";

        return Task.FromResult(new AuthSession
        {
            RedirectUri = new Uri(authUrl),
            TokenTask = _tcs.Task
        });
    }

    public void HandleCallback(Uri uri)
    {
        var result = ParseResult(uri);
        if (result != null)
        {
            _tcs?.TrySetResult(result);
        }
        else
        {
            _tcs?.TrySetException(new Exception("Auth failed: Missing tokens in callback"));
        }
    }

    public AuthResult? ParseResult(Uri uri)
    {
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var appToken = query["appToken"];
        var spotifyAccessToken = query["spotifyAccessToken"];
        var spotifyRefreshToken = query["spotifyRefreshToken"];
        var expiresInStr = query["expiresIn"];

        if (!string.IsNullOrEmpty(appToken) && !string.IsNullOrEmpty(spotifyAccessToken))
        {
            int.TryParse(expiresInStr, out var expiresIn);
            return new AuthResult
            {
                AppToken = appToken,
                SpotifyToken = new AuthorizationCodeTokenResponse
                {
                    AccessToken = spotifyAccessToken,
                    RefreshToken = spotifyRefreshToken,
                    ExpiresIn = expiresIn,
                    TokenType = "Bearer"
                }
            };
        }

        return null;
    }

    public void OpenUrl(Uri uri)
    {
        if (IsBrowser)
        {
            // In Browser, we can use JS to redirect the top window
            // This is safer than popups which are often blocked
            OpenUrlBrowser(uri.ToString());
        }
        else
        {
            // In Desktop/Mobile, use the platform's default browser or Launcher
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
    }

    private void OpenUrlBrowser(string url)
    {
        // Fallback for when we don't have JS Interop set up yet, 
        // though in Avalonia Browser it should be done via JSHost.
        // For now, we'll try to use a simple approach if possible, 
        // but typically you'd want:
        // System.Runtime.InteropServices.JavaScript.JSHost.Window.Location.Href = url;
        
        try 
        {
            // This is a common hack for opening URLs in WASM if not using JSImport
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch 
        {
            // If it fails, we might need real JS Interop
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