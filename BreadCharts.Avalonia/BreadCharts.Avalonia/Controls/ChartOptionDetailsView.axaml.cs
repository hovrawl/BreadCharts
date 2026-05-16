using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BreadCharts.Avalonia.Controls;

public partial class ChartOptionDetailsView : UserControl
{
    public const string ViewName = "ChartOptionDetails";
    
    public ChartOptionDetailsView()
    {
        InitializeComponent();
    }

    public void Next(object source, RoutedEventArgs args)
    {
        ImageCarousel.Next();
    }

    public void Previous(object source, RoutedEventArgs args)
    {
        ImageCarousel.Previous();
    }
}