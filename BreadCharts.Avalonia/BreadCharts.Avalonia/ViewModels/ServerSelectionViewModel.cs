using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BreadCharts.Avalonia.Services;
using BreadCharts.Avalonia.Views.User;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BreadCharts.Avalonia.ViewModels;

public partial class ServerSelectionViewModel : ViewModelBase
{
    private readonly ServerDiscoveryService _discoveryService;
    private readonly ApiClient _apiClient;
    private readonly NavigationService _navigationService;

    public ObservableCollection<ServerInfo> Servers => _discoveryService.DiscoveredServers;

    [ObservableProperty]
    private ServerInfo? _selectedServer;

    [ObservableProperty]
    private string? _manualAddress;

    public bool IsManualEntryVisible => Servers.Count == 0;

    public ServerSelectionViewModel(
        ServerDiscoveryService discoveryService, 
        ApiClient apiClient,
        NavigationService navigationService)
    {
        _discoveryService = discoveryService;
        _apiClient = apiClient;
        _navigationService = navigationService;
        
        _discoveryService.DiscoveredServers.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsManualEntryVisible));
        
        _discoveryService.StartListening();
    }

    [RelayCommand]
    private void SelectServer()
    {
        string? address = null;
        if (SelectedServer != null)
        {
            address = SelectedServer.Address;
        }
        else if (!string.IsNullOrWhiteSpace(ManualAddress))
        {
            address = ManualAddress;
        }

        if (address != null)
        {
            _apiClient.SetBaseAddress(address);
            _discoveryService.StopListening();
            _navigationService.Navigate(AuthView.ViewName);
        }
    }
}
