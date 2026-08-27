using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Vanta.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Vanta;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        StartupTrace.Mark("App constructor: begin");
        UnhandledException += (_, eventArgs) => StartupTrace.Mark($"Unhandled exception: {eventArgs.Exception}");
        RequestedTheme = ApplicationTheme.Dark;
        InitializeComponent();
        StartupTrace.Mark("App constructor: initialized");
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        StartupTrace.Mark("OnLaunched: begin");
        MainWindow = new MainWindow();
        StartupTrace.Mark("OnLaunched: window constructed");
        MainWindow.Activate();
        ((MainWindow)MainWindow).ConfigureWindow();
        StartupTrace.Mark($"OnLaunched: window activated, HWND=0x{WinRT.Interop.WindowNative.GetWindowHandle(MainWindow).ToInt64():X}");
    }
}
