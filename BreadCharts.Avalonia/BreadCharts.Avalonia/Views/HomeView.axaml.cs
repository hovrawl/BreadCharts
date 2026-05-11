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
            var tokenResponse = await session.TokenTask;
        
            // Proceed with the token (e.g., Navigate to profile)
            OnAuthSuccess(tokenResponse);
        }
        catch (Exception ex)
        {
            // Handle login failure or cancellation
        }
    }

    private async Task OnAuthSuccess(AuthorizationCodeTokenResponse tokenResponse)
    {
        // This is where we take user token, create spotify client,
        // setup a user and navigate back to home
        if (DataContext is not MainViewModel viewModel) return;
        
        // Give user token to VM to handle
        await viewModel.HandleUserToken(tokenResponse);
        // Navigate back to home
        viewModel.NavService.Navigate(ViewName);
    }
}