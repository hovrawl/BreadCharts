using System.Collections.Concurrent;
using BreadCharts.Core.Infrastructure;
using BreadCharts.Core.Models;
using BreadCharts.Core.Models.Mapping;
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

    public async Task<SpotifyClient> GetClient(string userId, string accessToken, string? refreshToken = null)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("accessToken is required", nameof(accessToken));

        // If a client exists for this user and current access token, return it.
        var cacheKey = $"{userId}:{accessToken}";
        if (_clients.TryGetValue(cacheKey, out var existing))
        {
            return existing;
        }

        // Configure an authenticator that will auto-refresh tokens when 401/expired
        IAuthenticator authenticator = !string.IsNullOrEmpty(refreshToken)
            ? new AuthorizationCodeAuthenticator(_clientId, _clientSecret, new AuthorizationCodeTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = 3600 // Spotify typically issues 1-hour tokens; actual value isn't critical here
            })
            : new TokenAuthenticator(accessToken, "Bearer");

        var config = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(authenticator);

        var client = new SpotifyClient(config);
        _clients[cacheKey] = client;
        return client;
    }

    public async Task<UserProfile?> GetUserProfile(string userId, string accessToken, string? refreshToken = null)
    {
        var spotify = await GetClient(userId, accessToken, refreshToken);
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
        var spotify = await GetClient(userId, accessToken, refreshToken);
        var returnList = new List<FullTrack>();
        var tracksResponse = await spotify.UserProfile.GetTopTracks(new UsersTopItemsRequest(TimeRange.MediumTerm));
        if (tracksResponse?.Items == null) return returnList;
        return tracksResponse.Items.ToList();
    }

    public async Task<List<ChartOption>> Search(string userId, string accessToken, string? refreshToken, string searchTerm, int page = -1)
    {
        var spotify = await GetClient(userId, accessToken, refreshToken);
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
                returnList.Add(artist.ToChartOption());
            }
        }
        if (searchResponse.Albums.Items != null)
        {
            foreach (var album in searchResponse.Albums.Items)
            {
                returnList.Add(album.ToChartOption());
            }
        }
        if (searchResponse.Tracks?.Items != null)
        {
            foreach (var track in searchResponse.Tracks.Items)
            {
                returnList.Add(track.ToChartOption());
            }
        }
        if (searchResponse.Playlists.Items != null)
        {
            foreach (var playlist in searchResponse.Playlists.Items)
            {
                if (playlist == null) continue;
                returnList.Add(playlist.ToChartOption());
            }
        }
        return returnList;
    }
    
    public async Task<FullArtist?> GetArtist(string userId, string accessToken, string? refreshToken, string id)
    {
        var spotify = await GetClient(userId, accessToken, refreshToken);
        if (string.IsNullOrWhiteSpace(id)) return null;
        var artist = await spotify.Artists.Get(id);
        return artist;
    }

    public async Task<FullAlbum?> GetAlbum(string userId, string accessToken, string? refreshToken, string id)
    {
        var spotify = await GetClient(userId, accessToken, refreshToken);
        if (string.IsNullOrWhiteSpace(id)) return null;
        var album = await spotify.Albums.Get(id);
        return album;
    }

    public async Task<FullPlaylist?> GetPlaylist(string userId, string accessToken, string? refreshToken, string id)
    {
        var spotify = await GetClient(userId, accessToken, refreshToken);
        if (string.IsNullOrWhiteSpace(id)) return null;
        var playlist = await spotify.Playlists.Get(id);
        return playlist;
    }

    public async Task<List<ChartOption>> GetArtistTopTracksChartOptions(string userId, string accessToken, string? refreshToken, string artistId, string market = "US")
    {
        var spotify = await GetClient(userId, accessToken, refreshToken);
        var list = new List<ChartOption>();
        if (string.IsNullOrWhiteSpace(artistId)) return list;
        var top = await spotify.Artists.GetTopTracks(artistId, new ArtistsTopTracksRequest(market));
        if (top?.Tracks != null)
        {
            foreach (var t in top.Tracks)
            {
                list.Add(t.ToChartOption());
            }
        }
        return list;
    }

    public async Task<List<ChartOption>> GetArtistAlbumsChartOptions(string userId, string accessToken, string? refreshToken, string artistId)
    {
        var spotify = await GetClient(userId, accessToken, refreshToken);
        var list = new List<ChartOption>();
        if (string.IsNullOrWhiteSpace(artistId)) return list;
        var req = new ArtistsAlbumsRequest { Limit = 20 };
        var page = await spotify.Artists.GetAlbums(artistId, req);
        if (page?.Items != null)
        {
            foreach (var a in page.Items)
            {
                list.Add(a.ToChartOption());
            }
        }
        return list;
    }

    public async Task<List<ChartOption>> GetAlbumTracksChartOptions(string userId, string accessToken, string? refreshToken, string albumId)
    {
        var spotify = await GetClient(userId, accessToken, refreshToken);
        var list = new List<ChartOption>();
        if (string.IsNullOrWhiteSpace(albumId)) return list;
        var req = new AlbumTracksRequest { Limit = 50 };
        var page = await spotify.Albums.GetTracks(albumId, req);
        if (page?.Items != null)
        {
            foreach (var t in page.Items)
            {
                list.Add(t.ToChartOption());
            }
        }
        return list;
    }

    public async Task<List<ChartOption>> GetPlaylistTracksChartOptions(string userId, string accessToken, string? refreshToken, string playlistId)
    {
        var spotify = await GetClient(userId, accessToken, refreshToken);
        var list = new List<ChartOption>();
        if (string.IsNullOrWhiteSpace(playlistId)) return list;
        var req = new PlaylistGetItemsRequest { Limit = 50 };
        var page = await spotify.Playlists.GetItems(playlistId, req);
        if (page?.Items != null)
        {
            foreach (var it in page.Items)
            {
                if (it.Track is FullTrack ft)
                {
                    list.Add(ft.ToChartOption());
                }
            }
        }
        return list;
    }
}