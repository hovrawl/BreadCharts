using System.Collections.Generic;
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
    
    public SearchView()
    {
        InitializeComponent();
    }

    private void SearchQueryBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // debounce text changed
        // send query to spotify
    }

    private async Task<List<ChartOption>> RunSearchQuery(string query)
    {
        if (DataContext is not SearchViewModel vm) return null;
        
        var results = await vm.Search(query);
        
        return results;
    }
}