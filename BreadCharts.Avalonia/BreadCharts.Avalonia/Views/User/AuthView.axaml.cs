using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BreadCharts.Avalonia.ViewModels;

namespace BreadCharts.Avalonia.Views.User;

public partial class AuthView : UserControl
{
    public const string ViewName = "Auth";
    
    public AuthView()
    {
        InitializeComponent();
    }
}