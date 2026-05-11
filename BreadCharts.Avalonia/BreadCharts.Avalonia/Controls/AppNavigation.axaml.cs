using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BreadCharts.Avalonia.ViewModels;
using BreadCharts.Avalonia.Views;
using ProfileView = BreadCharts.Avalonia.Views.User.ProfileView;

namespace BreadCharts.Avalonia.Controls;

public partial class AppNavigation : UserControl
{
    // public List<ListBoxItem> NavigationItems = new List<ListBoxItem>();
    public AppNavigation()
    {
        InitializeComponent();
    }

    private void Init()
    {
        // Set navigation frame
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.NavService.SetFrame(NavFrame);
        }
        if (PageSelection.Items.Count > 0)
        {
            var homeItem = PageSelection.Items[0];
            PageSelection.SelectedItem = homeItem;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        Init();
    }

    private void PageSelection_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not ListBoxItem selectedItem) return;
        
        if (DataContext is not MainViewModel viewModel) return;
        
        var tag = selectedItem.Name ?? "";
        
        viewModel.NavService.Navigate(tag);
    }
}