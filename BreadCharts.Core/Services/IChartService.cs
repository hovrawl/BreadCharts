using BreadCharts.Core.Models;

namespace BreadCharts.Core.Services;

public interface IChartService
{
    
}

public class ChartService : IChartService
{
    private readonly ISpotifyClientService spotifyClientService;
    
    public ChartService(ISpotifyClientService spotifyClientService)
    {
        this.spotifyClientService = spotifyClientService;
    }
    
    public List<ChartOption> SearchChartOptions(string searchTerm)
    {
        return new List<ChartOption>();
    }
}