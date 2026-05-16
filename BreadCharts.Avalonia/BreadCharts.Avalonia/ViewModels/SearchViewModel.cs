using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BreadCharts.Avalonia.Controls;
using BreadCharts.Avalonia.Services;
using BreadCharts.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace BreadCharts.Avalonia.ViewModels;

public partial class SearchViewModel :ViewModelBase
{
    private readonly SpotifyService _spotifyService;
    private readonly AuthService _authService;
    private readonly NavigationService _navService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _searchQuery;
    
    
    public ObservableCollection<ChartOption> SearchResults { get; set; }
    
    public SearchViewModel (SpotifyService spotifyService, 
        AuthService authService,
        NavigationService navService,
        IServiceProvider serviceProvider)
    {
        _spotifyService = spotifyService;
        _authService = authService;
        _navService = navService;
        _serviceProvider = serviceProvider;
        SearchResults = [];
    }

    public async Task Search(string query)
    {
        var results =  await _spotifyService.Search(query);
         SearchResults.Clear();
        foreach (var result in results)
        {
            SearchResults.Add(result);
        }
    }
    
    public async Task NavigateToChartOptionsDetails(ChartOption option)
    {
        // 
        var vm = _serviceProvider.GetRequiredService<ChartOptionDetailsViewModel>();
        await vm.LoadChartOptionDetails(option);
        _navService.Navigate(ChartOptionDetailsView.ViewName, vm);
    }
}