using BreadCharts.Web.Data;

namespace BreadCharts.Web.Services;

public record SubmittedSongView(string TrackId, string TrackName, string SubmittedByUserId, int VoteCount, bool HasVoted);

public interface IVotingService
{
    // Raised whenever submissions or votes change, so listeners can refresh UI
    event Action? Changed;

    Task<List<SubmittedSongView>> GetSubmissionsAsync(string currentUserId, CancellationToken ct = default);
    Task<(bool ok, string message)> SubmitAsync(string currentUserId, string trackId, string trackName, CancellationToken ct = default);
    Task<(bool ok, string message)> VoteAsync(string currentUserId, string trackId, CancellationToken ct = default);
    Task<(bool ok, string message)> UnvoteAsync(string currentUserId, string trackId, CancellationToken ct = default);
}
