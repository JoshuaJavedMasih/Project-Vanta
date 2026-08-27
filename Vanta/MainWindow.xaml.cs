using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Vanta.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Vanta;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        StartupTrace.Mark("MainWindow: begin");
        InitializeComponent();
        StartupTrace.Mark($"MainWindow: XAML initialized, HWND=0x{WinRT.Interop.WindowNative.GetWindowHandle(this).ToInt64():X}");
        Closed += (_, _) => StartupTrace.Mark("MainWindow: Closed event");

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        RootFrame.Navigate(typeof(MainPage));
        StartupTrace.Mark("MainWindow: main page navigated");
    }

    internal void ConfigureWindow()
    {
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonForegroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 244, 246, 253);
            titleBar.ButtonInactiveForegroundColor = Microsoft.UI.ColorHelper.FromArgb(150, 155, 164, 184);
            titleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(22, 255, 255, 255);
        }

        var desired = new SizeInt32(1440, 900);
        AppWindow.Resize(desired);
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        if (display is not null)
        {
            var x = display.WorkArea.X + Math.Max(0, (display.WorkArea.Width - desired.Width) / 2);
            var y = display.WorkArea.Y + Math.Max(0, (display.WorkArea.Height - desired.Height) / 2);
            AppWindow.Move(new PointInt32(x, y));
        }

        StartupTrace.Mark("MainWindow: configured");
    }
}
