using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreadCharts.Web.Data;

public class SubmittedSong
{
    [Key]
    [MaxLength(100)]
    public string TrackId { get; set; } = null!; // Spotify Track ID (PK)

    [MaxLength(512)]
    public string TrackName { get; set; } = string.Empty; // denormalized for display

    [MaxLength(450)]
    public string SubmittedByUserId { get; set; } = null!;

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<SongVote> Votes { get; set; } = new List<SongVote>();
}

public class SongVote
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string TrackId { get; set; } = null!;

    [ForeignKey(nameof(TrackId))]
    public SubmittedSong Song { get; set; } = null!;

    [MaxLength(450)]
    public string UserId { get; set; } = null!;

    public DateTime VotedAtUtc { get; set; } = DateTime.UtcNow;
}
