using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BreadCharts.Avalonia.Views;

namespace BreadCharts.Avalonia.Controls;

public partial class AppNavigation : UserControl
{
    // public List<ListBoxItem> NavigationItems = new List<ListBoxItem>();
    public AppNavigation()
    {
        InitializeComponent();
        Init();
    }

    private void Init()
    {
        if (PageSelection.Items.Count > 0)
        {
            var homeItem = PageSelection.Items[0];
            PageSelection.SelectedItem = homeItem;
        }
    }

    private void PageSelection_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not ListBoxItem selectedItem) return;

        var tag = selectedItem.Name?.ToLower() ?? "";
        switch (tag)
        {
            case "home":
            {
                NavFrame.Navigate(typeof(HomeView));
                break;
            }
            case "search":
            {
                NavFrame.Navigate(typeof(SearchView));
                break;
            }
            case "profile":
            {
                NavFrame.Navigate(typeof(ProfileView));
                break;
            }
            default:
                break;
        }
    }
}