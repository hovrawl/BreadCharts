using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BreadCharts.Core.Services;

namespace BreadCharts.Avalonia.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private string? _appToken;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public void SetAppToken(string? token)
    {
        _appToken = token;
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<List<SubmittedSongView>> GetSubmissionsAsync()
    {
        return await _http.GetFromJsonAsync<List<SubmittedSongView>>("/api/voting/submissions") ?? new();
    }

    public async Task<(bool ok, string message)> SubmitAsync(string trackId, string trackName)
    {
        var response = await _http.PostAsJsonAsync("/api/voting/submit", new { trackId, trackName });
        var message = await response.Content.ReadAsStringAsync();
        return (response.IsSuccessStatusCode, message);
    }

    public async Task<(bool ok, string message)> VoteAsync(string trackId)
    {
        var response = await _http.PostAsync($"/api/voting/vote/{trackId}", null);
        var message = await response.Content.ReadAsStringAsync();
        return (response.IsSuccessStatusCode, message);
    }

    public async Task<(bool ok, string message)> UnvoteAsync(string trackId)
    {
        var response = await _http.DeleteAsync($"/api/voting/vote/{trackId}");
        var message = await response.Content.ReadAsStringAsync();
        return (response.IsSuccessStatusCode, message);
    }
}
