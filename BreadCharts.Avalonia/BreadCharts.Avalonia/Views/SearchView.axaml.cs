using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BreadCharts.Avalonia.ViewModels;
using BreadCharts.Core.Models;

namespace BreadCharts.Avalonia.Views;

public partial class SearchView : UserControl
{
    public const string ViewName = "Search";
    private const int DebounceDelayMs = 300;
    private CancellationTokenSource? _debounceCts;
    
    public SearchView()
    {
        InitializeComponent();
    }

    private async void SearchQueryBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // Cancel previous search if still pending
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        if (sender is not TextBox textBox) return;
        var query = textBox.Text;

        if (string.IsNullOrWhiteSpace(query)) return;

        try
        {
            // Wait for debounce delay
            await Task.Delay(DebounceDelayMs, _debounceCts.Token);

            // Execute search query
            await RunSearchQuery(query);
        }
        catch (TaskCanceledException)
        {
            // Debounce was cancelled, ignore
        }
    }

    private async Task RunSearchQuery(string query)
    {
        if (DataContext is not SearchViewModel vm) return;
        
        await vm.Search(query);
    }

    private void SearchResultsBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Show details for chart option
        if (sender is not ListBox { SelectedItem: ChartOption option })
        {
            return;
        }
        
        if (DataContext is not SearchViewModel vm) return;
        
        // Nav Frame 
        vm.NavigateToChartOptionsDetails(option);
        
    }
}