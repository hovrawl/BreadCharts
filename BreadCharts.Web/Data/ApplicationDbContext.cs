using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BreadCharts.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<SubmittedSong> SubmittedSongs => Set<SubmittedSong>();
    public DbSet<SongVote> SongVotes => Set<SongVote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<SubmittedSong>(b =>
        {
            b.HasKey(s => s.TrackId);
            b.HasMany(s => s.Votes)
                .WithOne(v => v.Song)
                .HasForeignKey(v => v.TrackId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<SongVote>(b =>
        {
            b.HasIndex(v => new { v.TrackId, v.UserId }).IsUnique();
        });
    }
}