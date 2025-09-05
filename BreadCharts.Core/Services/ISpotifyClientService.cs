using BreadCharts.Core.Models;
using SpotifyAPI.Web;

namespace BreadCharts.Core.Services;

public interface ISpotifyClientService
{
    // Returns a cached or newly created SpotifyClient for a specific user
    Task<SpotifyClient> GetClient(string userId, string accessToken, string? refreshToken = null);

    // Convenience helpers that operate using the per-user client
    Task<UserProfile?> GetUserProfile(string userId, string accessToken, string? refreshToken = null);
    Task<List<FullTrack>> GetBasicTracks(string userId, string accessToken, string? refreshToken = null);
    Task<List<ChartOption>> Search(string userId, string accessToken, string? refreshToken, string searchTerm, int page = -1);

    // Navigable entity fetchers
    Task<FullArtist?> GetArtist(string userId, string accessToken, string? refreshToken, string id);
    Task<FullAlbum?> GetAlbum(string userId, string accessToken, string? refreshToken, string id);
    Task<FullPlaylist?> GetPlaylist(string userId, string accessToken, string? refreshToken, string id);

    // Child collections projected to ChartOptions for navigation
    Task<List<ChartOption>> GetArtistTopTracksChartOptions(string userId, string accessToken, string? refreshToken, string artistId, string market = "US");
    Task<List<ChartOption>> GetArtistAlbumsChartOptions(string userId, string accessToken, string? refreshToken, string artistId);
    Task<List<ChartOption>> GetAlbumTracksChartOptions(string userId, string accessToken, string? refreshToken, string albumId);
    Task<List<ChartOption>> GetPlaylistTracksChartOptions(string userId, string accessToken, string? refreshToken, string playlistId);
}