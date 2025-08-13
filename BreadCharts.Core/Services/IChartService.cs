using BreadCharts.Core.Models;

namespace BreadCharts.Core.Services;

public interface IChartService
{
    
}

public class ChartService : IChartService
{
    private readonly SpotifyAuthService spotifyAuthService;
    
    public ChartService(SpotifyAuthService spotifyAuthService)
    {
        this.spotifyAuthService = spotifyAuthService;
    }
    
    public List<ChartOption> SearchChartOptions(string searchTerm)
    {
        return new List<ChartOption>();
    }
}