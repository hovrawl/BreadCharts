using System.Collections.Concurrent;
using BreadCharts.Core.Infrastructure;
using BreadCharts.Core.Models;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;

namespace BreadCharts.Core.Services;

public class SpotifyClientService : ISpotifyClientService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private const int PageSize = 20;

    // Cache clients per user id
    private static readonly ConcurrentDictionary<string, SpotifyClient> _clients = new();

    public SpotifyClientService(IConfiguration configRoot)
    {
        var authServiceConfig = configRoot.GetSection("AuthServiceConfig").Get<AuthServiceConfig>();
        _clientId = authServiceConfig.ClientId;
        _clientSecret = authServiceConfig.ClientSecret;
    }

    public SpotifyClient GetClient(string userId, string accessToken, string? refreshToken = null)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("accessToken is required", nameof(accessToken));

        // If a client exists, return it. We keep it simple without token refresh for now.
        if (_clients.TryGetValue(userId, out var existing))
        {
            return existing;
        }

        var config = SpotifyClientConfig.CreateDefault().WithToken(accessToken);
        var client = new SpotifyClient(config);
        _clients[userId] = client;
        return client;
    }

    public async Task<UserProfile?> GetUserProfile(string userId, string accessToken, string? refreshToken = null)
    {
        var spotify = GetClient(userId, accessToken, refreshToken);
        var privateUser = await spotify.UserProfile.Current();
        if (privateUser == null) return null;
        return new UserProfile
        {
            Id = privateUser.Id,
            Name = privateUser.DisplayName
        };
    }

    public async Task<List<FullTrack>> GetBasicTracks(string userId, string accessToken, string? refreshToken = null)
    {
        var spotify = GetClient(userId, accessToken, refreshToken);
        var returnList = new List<FullTrack>();
        var tracksResponse = await spotify.UserProfile.GetTopTracks(new UsersTopItemsRequest(TimeRange.MediumTerm));
        if (tracksResponse?.Items == null) return returnList;
        return tracksResponse.Items.ToList();
    }

    public async Task<List<ChartOption>> Search(string userId, string accessToken, string? refreshToken, string searchTerm, int page = -1)
    {
        var spotify = GetClient(userId, accessToken, refreshToken);
        var returnList = new List<ChartOption>();
        if (string.IsNullOrWhiteSpace(searchTerm)) return returnList;

        var offset = page > 0 ? page * PageSize : 0;
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
                returnList.Add(new ChartOption { Id = artist.Id, Name = artist.Name, Type = ChartOptionType.Artist });
            }
        }
        if (searchResponse.Albums.Items != null)
        {
            foreach (var album in searchResponse.Albums.Items)
            {
                returnList.Add(new ChartOption { Id = album.Id, Name = album.Name, Type = ChartOptionType.Album });
            }
        }
        if (searchResponse.Tracks?.Items != null)
        {
            foreach (var track in searchResponse.Tracks.Items)
            {
                var trackName = $"{string.Join(", ", track.Artists.Select(art => art.Name))} - {track.Name}";
                returnList.Add(new ChartOption { Id = track.Id, Name = trackName, Type = ChartOptionType.Track });
            }
        }
        if (searchResponse.Playlists.Items != null)
        {
            foreach (var playlist in searchResponse.Playlists.Items)
            {
                if (playlist == null) continue;
                returnList.Add(new ChartOption { Id = playlist.Id, Name = playlist.Name, Type = ChartOptionType.Playlist });
            }
        }
        return returnList;
    }
}