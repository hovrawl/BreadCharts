using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BreadCharts.Avalonia.ViewModels;
using BreadCharts.Avalonia.Views;
using BreadCharts.Avalonia.Views.User;
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
            
            // Navigate directly to AuthView
            viewModel.NavService.Navigate(AuthView.ViewName);
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