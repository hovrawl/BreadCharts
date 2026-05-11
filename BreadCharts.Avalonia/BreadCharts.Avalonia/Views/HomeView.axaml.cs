using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BreadCharts.Avalonia.ViewModels;
using SpotifyAPI.Web;

namespace BreadCharts.Avalonia.Views;

public partial class HomeView : UserControl
{
    public const string ViewName = "Home";
    
    public HomeView()
    {
        InitializeComponent();
    }

    private async void AuthBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;

       
        // 1. Start the session
        var session = await viewModel.AuthService.BeginAuth();

        // 2. Open the browser/view
        viewModel.NavService.NavigateToWebUri(session.RedirectUri);

        try 
        {
            // 3. Cleanly await the result!
            var authResult = await session.TokenTask;
        
            // Proceed with the token
            await viewModel.HandleAuthResult(authResult);
            viewModel.NavService.Navigate(ViewName);
        }
        catch (Exception ex)
        {
            // Handle login failure or cancellation
        }
    }
}