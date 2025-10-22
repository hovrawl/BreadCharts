using BreadCharts.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BreadCharts.Web.Services;

public class VotingService(ApplicationDbContext db, IOptions<VotingOptions> options)
    : IVotingService
{
    private readonly ApplicationDbContext _db = db;
    private readonly VotingOptions _opt = options.Value ?? new VotingOptions();

    public event Action? Changed;
    private void NotifyChanged() => Changed?.Invoke();

    public async Task<List<SubmittedSongView>> GetSubmissionsAsync(string currentUserId, CancellationToken ct = default)
    {
        var list = await _db.SubmittedSongs
            .Include(s => s.Votes)
            .OrderByDescending(s => s.Votes.Count)
            .ThenBy(s => s.TrackName)
            .ToListAsync(ct);

        return list.Select(s => new SubmittedSongView(
            s.TrackId,
            s.TrackName,
            s.SubmittedByUserId,
            s.Votes.Count,
            s.Votes.Any(v => v.UserId == currentUserId)
        )).ToList();
    }

    public async Task<(bool ok, string message)> SubmitAsync(string currentUserId, string trackId, string trackName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currentUserId)) return (false, "Not authenticated");
        if (string.IsNullOrWhiteSpace(trackId)) return (false, "Invalid track");

        var existing = await _db.SubmittedSongs.Include(s => s.Votes).FirstOrDefaultAsync(s => s.TrackId == trackId, ct);
        if (existing is not null)
        {
            // Treat as a vote attempt for existing submission
            if (existing.Votes.Any(v => v.UserId == currentUserId))
            {
                return (true, "Already submitted and you have already voted");
            }
            var currentVotesExisting = await _db.SongVotes.CountAsync(v => v.UserId == currentUserId, ct);
            if (currentVotesExisting >= _opt.MaxVotesPerUser)
            {
                return (true, $"Song already submitted. Vote not added because you have reached the maximum of {_opt.MaxVotesPerUser} votes.");
            }
            _db.SongVotes.Add(new SongVote { TrackId = trackId, UserId = currentUserId, VotedAtUtc = DateTime.UtcNow });
            await _db.SaveChangesAsync(ct);
            NotifyChanged();
            return (true, "Song already submitted. Your vote has been added.");
        }

        var submission = new SubmittedSong
        {
            TrackId = trackId,
            TrackName = trackName ?? string.Empty,
            SubmittedByUserId = currentUserId,
            SubmittedAtUtc = DateTime.UtcNow
        };
        _db.SubmittedSongs.Add(submission);

        // Attempt to auto-vote for the submitter within limits
        var currentVotes = await _db.SongVotes.CountAsync(v => v.UserId == currentUserId, ct);
        bool addedVote = false;
        if (currentVotes < _opt.MaxVotesPerUser)
        {
            _db.SongVotes.Add(new SongVote { TrackId = trackId, UserId = currentUserId, VotedAtUtc = DateTime.UtcNow });
            addedVote = true;
        }

        await _db.SaveChangesAsync(ct);
        NotifyChanged();
        if (addedVote)
        {
            return (true, "Song submitted and your vote has been added.");
        }
        else
        {
            return (true, $"Song submitted. You have already used {currentVotes} of {_opt.MaxVotesPerUser} votes, so your vote was not added.");
        }
    }

    public async Task<(bool ok, string message)> VoteAsync(string currentUserId, string trackId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currentUserId)) return (false, "Not authenticated");
        var submission = await _db.SubmittedSongs.Include(s => s.Votes).FirstOrDefaultAsync(s => s.TrackId == trackId, ct);
        if (submission is null) return (false, "Song not found");

        // Enforce max votes per user
        var currentVotes = await _db.SongVotes.CountAsync(v => v.UserId == currentUserId, ct);
        if (currentVotes >= _opt.MaxVotesPerUser)
        {
            return (false, $"You have reached the maximum of {_opt.MaxVotesPerUser} votes.");
        }

        if (submission.Votes.Any(v => v.UserId == currentUserId))
        {
            return (true, "Already voted");
        }
        _db.SongVotes.Add(new SongVote { TrackId = trackId, UserId = currentUserId, VotedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        NotifyChanged();
        return (true, "Voted");
    }

    public async Task<(bool ok, string message)> UnvoteAsync(string currentUserId, string trackId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currentUserId)) return (false, "Not authenticated");
        var vote = await _db.SongVotes.FirstOrDefaultAsync(v => v.TrackId == trackId && v.UserId == currentUserId, ct);
        if (vote is null) return (true, "Not voted");

        // Remove the user's vote
        _db.SongVotes.Remove(vote);
        await _db.SaveChangesAsync(ct);

        // If there are no remaining votes for this track, remove the submission as well
        var remainingVotes = await _db.SongVotes.CountAsync(v => v.TrackId == trackId, ct);
        if (remainingVotes == 0)
        {
            var submission = await _db.SubmittedSongs.FirstOrDefaultAsync(s => s.TrackId == trackId, ct);
            if (submission is not null)
            {
                _db.SubmittedSongs.Remove(submission);
                await _db.SaveChangesAsync(ct);
                NotifyChanged();
                return (true, "Unvoted and removed submission (no remaining votes)");
            }
        }

        NotifyChanged();
        return (true, "Unvoted");
    }
}