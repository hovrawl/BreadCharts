using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BreadCharts.Avalonia.Controls;

public partial class AppNavigation : UserControl
{
    public List<ListBoxItem> NavigationItems = new List<ListBoxItem>();
    public AppNavigation()
    {
        InitializeComponent();
        
    }

    // private void Init()
    // {
    //     NavigationItems.Add(new ListBoxItem
    //     {
    //         Content = "Home",
    //         Tag = "Home"
    //     });
    //     
    //     PageSelection.ItemsSource = NavigationItems;
    // }

}