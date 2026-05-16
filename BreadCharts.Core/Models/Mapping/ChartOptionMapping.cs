using SpotifyAPI.Web;

namespace BreadCharts.Core.Models.Mapping;

public static class ChartOptionMapping
{
    #region Chart Options

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
    
    #endregion

    #region Chart Option Details
    public static ChartOptionDetails ToChartOptionDetails(this FullPlaylist playlist)
    {
        return new ChartOptionDetails()
        {
            Id = playlist.Id,
            Name = playlist.Name,
            Description = playlist.Description,
            Images = playlist.Images,
        };
    }
    
    public static ChartOptionDetails ToChartOptionDetails(this FullArtist artist)
    {
        return new ChartOptionDetails()
        {
            Id = artist.Id,
            Name = artist.Name,
            Description = string.Join(", ", artist.Genres),
            Images = artist.Images,
        };
    }
    
    public static ChartOptionDetails ToChartOptionDetails(this FullTrack track)
    {
        return new ChartOptionDetails()
        {
            Id = track.Id,
            Name = track.Name,
            Description = track.Album.Name,
            Images = track.Album.Images,
        };
    }
    
    public static ChartOptionDetails ToChartOptionDetails(this FullAlbum album)
    {
        return new ChartOptionDetails()
        {
            Id = album.Id,
            Name = album.Name,
            Description = $"{album.AlbumType} - {album.Name}",
            Images = album.Images,
        };
    }
    
    #endregion
}