using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace BreadCharts.Avalonia.Android;

[Activity(
    Label = "BreadCharts.Avalonia.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
