using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
#if BROWSER
using System.Runtime.InteropServices.JavaScript;
#endif
using BreadCharts.Core.Models;
using SpotifyAPI.Web;

namespace BreadCharts.Avalonia.Services;

public partial class AuthService
{
    private string _apiBaseUrl = "https://127.0.0.1:7206"; 
    private string _redirectUri = "https://127.0.0.1:7206/auth/callback";
    private HttpClient? _httpClient;

    private static AuthResult? _pendingResult;
    private AuthResult? _currentResult;
    private TaskCompletionSource<AuthResult>? _tcs;

    public bool IsBrowser => OperatingSystem.IsBrowser();

    public AuthResult? CurrentResult => _currentResult;

    public AuthService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient;
        if (IsBrowser)
        {
            // Detect origin from browser if possible, otherwise rely on SetBaseAddress
            // For now, we'll allow it to be updated via a method or property
        }
    }

    public void SetApiBaseUrl(string apiBaseUrl)
    {
        _apiBaseUrl = apiBaseUrl.Replace("localhost", "127.0.0.1");
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
        _ = HandleCallbackAsync(uri);
    }

    public async Task HandleCallbackAsync(Uri? uri)
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
        if (result == null)
        {
            Log("Failed to parse auth result from callback");
            _tcs?.TrySetException(new Exception("Auth failed: Missing tokens in callback"));
            return;
        }

        if (!string.IsNullOrEmpty(result.Code))
        {
            Log($"Found exchange code: {result.Code}. Performing token exchange...");
            try
            {
                var client = _httpClient ?? new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };
                var response = await client.GetAsync($"/api/auth/exchange?code={result.Code}");
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseStub>();
                    if (authResponse != null)
                    {
                        Log("Token exchange successful");
                        int.TryParse(authResponse.ExpiresIn, out var expiresIn);
                        result = new AuthResult
                        {
                            AppToken = authResponse.AppToken,
                            SpotifyToken = new AuthorizationCodeTokenResponse
                            {
                                AccessToken = authResponse.SpotifyAccessToken ?? "",
                                RefreshToken = authResponse.SpotifyRefreshToken,
                                ExpiresIn = expiresIn,
                                TokenType = "Bearer"
                            },
                            UserId = authResponse.User?.Id
                        };
                    }
                }
                else
                {
                    Log($"Token exchange failed with status: {response.StatusCode}");
                    _tcs?.TrySetException(new Exception($"Auth failed: Token exchange failed with status {response.StatusCode}"));
                    return;
                }
            }
            catch (Exception ex)
            {
                Log($"Error during token exchange: {ex.Message}");
                _tcs?.TrySetException(ex);
                return;
            }
        }

        Log("Successfully obtained auth result");
        _currentResult = result;
        _tcs?.TrySetResult(result);
    }

    // Temporary stub for deserialization
    private class AuthResponseStub
    {
        public string AppToken { get; set; } = null!;
        public string? SpotifyAccessToken { get; set; }
        public string? SpotifyRefreshToken { get; set; }
        public string? ExpiresIn { get; set; }
        public UserSummaryStub? User { get; set; }
    }

    private class UserSummaryStub
    {
        public string Id { get; set; } = null!;
    }

    public AuthResult? ParseResult(Uri uri)
    {
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var code = query["code"];
        var appToken = query["appToken"];
        var spotifyAccessToken = query["spotifyAccessToken"];
        var spotifyRefreshToken = query["spotifyRefreshToken"];
        var expiresInStr = query["expiresIn"];

        if (!string.IsNullOrEmpty(code))
        {
            return new AuthResult { Code = code };
        }

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
    public string? Code { get; set; }
    public string AppToken { get; set; } = "";
    public string? UserId { get; set; }
    public AuthorizationCodeTokenResponse SpotifyToken { get; set; } = null!;
}

public class AuthSession
{
    public Uri RedirectUri { get; init; }
    public Task<AuthResult> TokenTask { get; init; }
}