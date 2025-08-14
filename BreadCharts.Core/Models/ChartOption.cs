namespace BreadCharts.Core.Models;

public class ChartOption
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public ChartOptionType Type { get; set; }

    public override string ToString()
    {
        return Name;
    }
}

public enum ChartOptionType
{
    Track,
    Album,
    Artist,
    Playlist
}