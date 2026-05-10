using BreadCharts.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BreadCharts.Avalonia.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] private UserProfile? _currentUser;
}
