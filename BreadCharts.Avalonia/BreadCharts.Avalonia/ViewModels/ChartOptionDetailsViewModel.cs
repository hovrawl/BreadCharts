using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using BreadCharts.Avalonia.Services;
using BreadCharts.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BreadCharts.Avalonia.ViewModels;

public partial class ChartOptionDetailsViewModel : ViewModelBase
{
    private readonly SpotifyService _spotifyService;
    private readonly ImageService _imageService;

    [ObservableProperty]
    private ChartOption _chartOption;
    
    private ObservableCollection<ChartOption> _relatedChartOptions;
    
    [ObservableProperty]
    private ChartOptionDetails _details;
    
    public ObservableCollection<Bitmap> LoadedImages { get; } = new();

    public ChartOptionDetailsViewModel(SpotifyService spotifyServiceService, 
        ImageService imageService)
    {
        _spotifyService = spotifyServiceService;
        _imageService = imageService;
        _relatedChartOptions = new ObservableCollection<ChartOption>();
    }
    
    public async Task LoadChartOptionDetails(ChartOption option)
    {
        ChartOption = option;
        // load more details
        Details = await _spotifyService.LoadChartOptionDetails(option);
        
        // Clear previous images and load new ones one by one
        LoadedImages.Clear();
        if (Details?.Images != null)
        {
            foreach (var img in Details.Images)
            {
                if (!string.IsNullOrEmpty(img.Url))
                {
                    // Asynchronously load image from web
                    var bitmap = await _imageService.GetImage(img.Url);
                    // Adding to the collection one at a time updates the UI immediately
                    LoadedImages.Add(bitmap);
                }
            }
        }
        
        // load related chart options
        _relatedChartOptions.Clear();
        var relatedOptions = await _spotifyService.GetRelatedChartOptions(option);
        foreach (var relatedOption in relatedOptions)
        {
            _relatedChartOptions.Add(relatedOption);
        }
    }
}