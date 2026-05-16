using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using SpotifyAPI.Web.Http;

namespace BreadCharts.Avalonia.Services;

public class ImageService
{
    private readonly HttpClient _httpClient;
    
    private Dictionary<string, Bitmap> _images = new();

    public ImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    private async Task LoadFromUrl(string url)
    {
        
        var bytes = await _httpClient.GetByteArrayAsync(url);
        using var stream = new MemoryStream(bytes);
        var bmp = new Bitmap(stream);
        _images.TryAdd(url, bmp);
    }
    
    public async Task<Bitmap> GetImage(string url)
    {
        if (_images.TryGetValue(url, out var bmp))
        {
            return bmp;
        }
        
        await LoadFromUrl(url);
        return _images[url];
    }


}