using System.Collections.Generic;
using System.Threading.Tasks;
using BreadCharts.Avalonia.Services;
using BreadCharts.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BreadCharts.Avalonia.ViewModels;

public partial class SearchViewModel :ViewModelBase
{
    private readonly SpotifyService _spotifyService;
    private readonly AuthService _authService;

    [ObservableProperty]
    private string _searchQuery;
    
    public SearchViewModel (SpotifyService spotifyService, AuthService authService)
    {
        _spotifyService = spotifyService;
        _authService = authService;
    }

    public async Task<List<ChartOption>> Search(string query)
    {
        return null;
        //return await _spotifyService.Search(CurrentUser.Id, query);
    }
}