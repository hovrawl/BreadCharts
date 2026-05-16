using SpotifyAPI.Web;

namespace BreadCharts.Core.Models;

public class ChartOptionDetails
{
    public string Id { get; set; } = "";
    
    public string Name { get; set; } = "";
    
    public string Description { get; set; } = "";
    
    public List<Image> Images { get; set; }
}