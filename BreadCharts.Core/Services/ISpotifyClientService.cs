using BreadCharts.Core.Models;
using SpotifyAPI.Web;

namespace BreadCharts.Core.Services;

public interface ISpotifyClientService
{
    // Returns a cached or newly created SpotifyClient for a specific user
    SpotifyClient GetClient(string userId, string accessToken, string? refreshToken = null);

    // Convenience helpers that operate using the per-user client
    Task<UserProfile?> GetUserProfile(string userId, string accessToken, string? refreshToken = null);
    Task<List<FullTrack>> GetBasicTracks(string userId, string accessToken, string? refreshToken = null);
    Task<List<ChartOption>> Search(string userId, string accessToken, string? refreshToken, string searchTerm, int page = -1);
}