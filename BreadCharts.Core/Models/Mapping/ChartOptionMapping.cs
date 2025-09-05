using SpotifyAPI.Web;

namespace BreadCharts.Core.Models.Mapping;

public static class ChartOptionMapping
{
    public static ChartOption ToChartOption(this FullArtist artist)
    {
        return new ChartOption
        {
            Id = artist.Id,
            Name = artist.Name, 
            Type = ChartOptionType.Artist
        };
    }
    
    public static ChartOption ToChartOption(this SimpleAlbum album)
    {
        return new ChartOption
        {
            Id = album.Id,
            Name = album.Name,
            Type = ChartOptionType.Album
        };
    }

    public static ChartOption ToChartOption(this FullTrack track)
    {
        var trackName = $"{string.Join(", ", track.Artists.Select(art => art.Name))} - {track.Name}";
        return new ChartOption
        {
            Id = track.Id,
            Name = trackName,
            Type = ChartOptionType.Track
        };
    }

    public static ChartOption ToChartOption(this SimpleTrack track)
    {
        var artists = track.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string>();
        var trackName = $"{string.Join(", ", artists)} - {track.Name}";
        return new ChartOption
        {
            Id = track.Id,
            Name = trackName,
            Type = ChartOptionType.Track
        };
    }

    public static ChartOption ToChartOption(this FullPlaylist playlist)
    {
        return new ChartOption
        {
            Id = playlist.Id,
            Name = playlist.Name,
            Type = ChartOptionType.Playlist
        };
    }
}