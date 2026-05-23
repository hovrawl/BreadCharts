using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if BROWSER
using System.Runtime.InteropServices.JavaScript;
#endif
using BreadCharts.Core.Models;
using SpotifyAPI.Web;

namespace BreadCharts.Avalonia.Services;

public partial class AuthService
{
    private string _apiBaseUrl = "https://localhost:7206"; 
    private string _redirectUri = "https://localhost:7206/auth/callback";

    private static AuthResult? _pendingResult;
    private AuthResult? _currentResult;
    private TaskCompletionSource<AuthResult>? _tcs;

    public bool IsBrowser => OperatingSystem.IsBrowser();

    public AuthResult? CurrentResult => _currentResult;

    public AuthService()
    {
        if (IsBrowser)
        {
            // Detect origin from browser if possible, otherwise rely on SetBaseAddress
            // For now, we'll allow it to be updated via a method or property
        }
    }

    public void SetApiBaseUrl(string apiBaseUrl)
    {
        _apiBaseUrl = apiBaseUrl;
    }

    public void SetRedirectBase(string baseAddress)
    {
        Log($"SetRedirectBase called with: {baseAddress}");
        var suffix = IsBrowser ? "auth_callback.html" : "auth/callback";
        _redirectUri = $"{baseAddress}/{suffix}";
        Log($"Redirect URI set to: {_redirectUri}");
    }

    public static void SetPendingResult(AuthResult result)
    {
        _pendingResult = result;
    }

#if BROWSER
    [JSExport]
#endif
    public static void OnAuthCompleted(string url)
    {
        Log($"OnAuthCompleted called with URL: {url}");
        // This is called from JS when the popup sends a message
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            Log("Successfully parsed URI, invoking AuthCompleted event");
            // We need to find the active AuthService instance to notify it.
            // Since AuthService is a singleton in our app, we can store it in a static field
            // or use a static event.
            AuthCompleted?.Invoke(null, uri);
        }
        else
        {
            Log($"Failed to parse URI: {url}");
        }
    }

#if BROWSER
    [JSExport]
#endif
    public static void Log(string message)
    {
        Console.WriteLine($"[JS] {message}");
    }

    private static event EventHandler<Uri>? AuthCompleted;

    public Task<AuthSession> BeginAuth()
    {
        Log("BeginAuth called");
        _tcs = new TaskCompletionSource<AuthResult>();

        if (_pendingResult != null)
        {
            Log("Found pending result, completing task immediately");
            var result = _pendingResult;
            _pendingResult = null;
            _tcs.SetResult(result);
        }

        if (IsBrowser)
        {
            Log("Subscribing to AuthCompleted event for browser");
            AuthCompleted += OnAuthCompletedInternal;
        }

        var authUrl = $"{_apiBaseUrl}/api/auth/spotify?redirectUrl={Uri.EscapeDataString(_redirectUri)}";
        Log($"Constructed Auth URL: {authUrl}");

        return Task.FromResult(new AuthSession
        {
            RedirectUri = new Uri(authUrl),
            TokenTask = _tcs.Task
        });
    }

    private void OnAuthCompletedInternal(object? sender, Uri uri)
    {
        Log($"OnAuthCompletedInternal triggered with URI: {uri}");
        AuthCompleted -= OnAuthCompletedInternal;
        HandleCallback(uri);
    }

    public void HandleCallback(Uri? uri)
    {
        Log($"HandleCallback called with URI: {uri}");
        if (uri == null) return;
        
        // Use a more flexible check for the callback URI since it might contain auth_callback.html or auth/callback
        if (!uri.ToString().Contains("auth") || (!uri.ToString().Contains("callback") && !uri.ToString().Contains("html"))) 
        {
            Log("URI does not appear to be a callback, ignoring");
            return;
        }
        
        var result = ParseResult(uri);
        if (result != null)
        {
            Log("Successfully parsed auth result from callback");
            _currentResult = result;
            _tcs?.TrySetResult(result);
        }
        else
        {
            Log("Failed to parse auth result from callback");
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
#if BROWSER
        OpenPopupBrowser(uri.ToString());
#else
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = uri.ToString(),
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
#endif
    }

    private void OpenPopupBrowser(string url)
    {
#if BROWSER
        _ = OpenPopupBrowserAsync(url);
#endif
    }

    private async Task OpenPopupBrowserAsync(string url)
    {
#if BROWSER
        await JSHost.ImportAsync("main.js", "../main.js");
        BrowserInterop.OpenPopup(url);
#endif
    }

    public async Task<UserProfile> InitUser(AuthorizationCodeTokenResponse tokenResponse)
    {
        if (tokenResponse == null)
            throw new ArgumentNullException(nameof(tokenResponse), "Token response cannot be null");
        
        var userProfile = await GetUserProfile(tokenResponse.AccessToken);
        if (userProfile == null)
            throw new InvalidOperationException("Failed to retrieve user profile from Spotify API");

        _currentResult = new AuthResult 
        { 
            SpotifyToken = tokenResponse,
            UserId = userProfile.Id
        };
        
        return userProfile;
    }

    private async Task<UserProfile?> GetUserProfile(string accessToken)
    {
        var spotifyClient = new SpotifyClient(accessToken);
        var privateUser = await spotifyClient.UserProfile.Current();
        if (privateUser == null) return null;
        
        return new UserProfile
        {
            Id = privateUser.Id,
            Name = privateUser.DisplayName,
        };
    }
}

#if BROWSER
public partial class BrowserInterop
{
    [JSImport("openUrl", "main.js")]
    public static partial void OpenUrl(string url);

    [JSImport("openPopup", "main.js")]
    public static partial void OpenPopup(string url);
}
#endif

public class AuthResult
{
    public string AppToken { get; set; } = "";
    public string? UserId { get; set; }
    public AuthorizationCodeTokenResponse SpotifyToken { get; set; } = null!;
}

public class AuthSession
{
    public Uri RedirectUri { get; init; }
    public Task<AuthResult> TokenTask { get; init; }
}