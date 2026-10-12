using System.Windows;
using MindAttic.Capture.Wpf;

namespace MindAttic.Browser;

/// <summary>
/// MindAttic Browser: a WebView2 browser for everyday browsing that can screenshot (with a crop lightbox) or record
/// any page straight to the clipboard, and can serve a local site folder as if it were deployed.
/// <code>
/// MindAttic.Browser.exe https://mindattic.com -capture video
/// MindAttic.Browser.exe D:\Projects\MindAttic\MindAttic.Web\mindattic.com -capture screen 5
/// </code>
/// </summary>
public partial class App : Application
{
    internal static BrowserSettingsStore SettingsStore { get; } = new();
    internal static BrowserSettings Settings { get; private set; } = new();
    internal static WpfCapture CaptureHandle { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Settings = SettingsStore.Load();
        var target = BrowserTarget.Parse(e.Args, home: Settings.HomePage, searchUrl: Settings.SearchUrl);
        var folder = Settings.ResolvedCaptureFolder();
        CaptureHandle = CaptureWpf.Init(this, e.Args, o =>
        {
            o.AppName = target.AppName;
            o.OutputRoot = folder;
        });
        MainWindow = new MainWindow(target);
        MainWindow.Show();
    }
}
