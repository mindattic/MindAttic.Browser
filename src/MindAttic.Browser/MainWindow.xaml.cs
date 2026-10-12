using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MindAttic.Capture;
using MindAttic.Capture.WebView2;
using Microsoft.Web.WebView2.Core;
using CaptureApi = MindAttic.Capture.Capture;

namespace MindAttic.Browser;

public partial class MainWindow : Window
{
    private const string RecordIcon = "", StopIcon = "", StarIcon = "", StarFilledIcon = "";

    private readonly BrowserTarget _target;
    private readonly DispatcherTimer _stateTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private bool _busyRecording;

    internal MainWindow(BrowserTarget target)
    {
        _target = target;
        InitializeComponent();
        Address.Text = target.Url;
        Loaded += async (_, _) => await InitializeWebViewAsync();
        _stateTimer.Tick += (_, _) => UpdateCaptureState();
        _stateTimer.Start();
    }

    private static BrowserSettings Settings => App.Settings;

    private async Task InitializeWebViewAsync()
    {
        try
        {
            var profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MindAttic", "Browser");
            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: profile);
            await Web.EnsureCoreWebView2Async(env);
        }
        catch (Exception ex) when (ex is WebView2RuntimeNotFoundException or System.Runtime.InteropServices.COMException)
        {
            Status.Text = $"WebView2 runtime unavailable: {ex.Message}";
            return;
        }
        var core = Web.CoreWebView2;
        var bridge = new WebViewBridgeOptions();
        if (_target.SiteFolder is { } folder && _target.VirtualHost is { } host)
        {
            // DenyCors: the site loads its own files, but a page from anywhere else on the web can't fetch the
            // served folder (often a repo root with .git and config files) or drive capture through the bridge.
            core.SetVirtualHostNameToFolderMapping(host, folder, CoreWebView2HostResourceAccessKind.DenyCors);
            bridge.AllowedOrigins.Add("https://" + host);
            Status.Text = $"Serving {folder} as https://{host}/";
        }
        await CaptureWebView2.AttachAsync(core, bridge);
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            core.Navigate(e.Uri);
        };
        core.SourceChanged += (_, _) =>
        {
            if (!Address.IsKeyboardFocusWithin) Address.Text = core.Source;
            UpdateStar();
        };
        core.HistoryChanged += (_, _) =>
        {
            BackButton.IsEnabled = core.CanGoBack;
            ForwardButton.IsEnabled = core.CanGoForward;
        };
        core.DocumentTitleChanged += (_, _) =>
            Title = string.IsNullOrWhiteSpace(core.DocumentTitle) ? "MindAttic Browser" : $"{core.DocumentTitle} — MindAttic Browser";
        core.NavigationCompleted += (_, e) =>
        {
            if (!e.IsSuccess) Status.Text = $"Could not load {core.Source} ({e.WebErrorStatus}).";
        };
        BackButton.IsEnabled = ForwardButton.IsEnabled = false;
        core.Navigate(_target.Url);
    }

    private string? CurrentUrl => Web.CoreWebView2?.Source;

    private void Navigate(string text)
    {
        if (Web.CoreWebView2 is null) return;
        var url = BrowserTarget.NormalizeUrl(text, Settings.SearchUrl);
        Address.Text = url;
        Web.CoreWebView2.Navigate(url);
        Web.Focus();
    }

    private void OnAddressKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Navigate(Address.Text);
        else if (e.Key == Key.Escape) Address.Text = CurrentUrl ?? Address.Text;
    }

    private void OnAddressFocused(object sender, KeyboardFocusChangedEventArgs e) => Address.SelectAll();
    private void OnBack(object sender, RoutedEventArgs e) { if (Web.CoreWebView2?.CanGoBack == true) Web.CoreWebView2.GoBack(); }
    private void OnForward(object sender, RoutedEventArgs e) { if (Web.CoreWebView2?.CanGoForward == true) Web.CoreWebView2.GoForward(); }
    private void OnReload(object sender, RoutedEventArgs e) => Web.CoreWebView2?.Reload();
    private void OnHome(object sender, RoutedEventArgs e) => Navigate(_target.SiteFolder is not null ? _target.Url : Settings.HomePage);
    private void OnFocusAddress(object sender, RoutedEventArgs e) => Address.Focus();
    private void OnDevTools(object sender, RoutedEventArgs e) => Web.CoreWebView2?.OpenDevToolsWindow();

    // ---------------------------------------------------------------- screenshot

    /// <summary>Shoots the page, opens the crop lightbox, then saves the kept part and puts it on the clipboard.</summary>
    private async void OnScreenshot(object sender, RoutedEventArgs e)
    {
        if (Web.CoreWebView2 is null) return;
        BitmapSource shot;
        try
        {
            using var png = new MemoryStream();
            await Web.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, png);
            png.Position = 0;
            var frame = BitmapFrame.Create(png, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            frame.Freeze();
            shot = frame;
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException or NotSupportedException)
        {
            Status.Text = $"Screenshot failed: {ex.Message}";
            return;
        }

        var keep = new PixelBox(0, 0, shot.PixelWidth, shot.PixelHeight);
        if (Settings.CropScreenshots)
        {
            var lightbox = new CropWindow(shot, this);
            if (lightbox.ShowDialog() != true || lightbox.Result is not { } box)
            {
                Status.Text = "Screenshot discarded.";
                return;
            }
            keep = box;
        }
        BitmapSource image = keep == new PixelBox(0, 0, shot.PixelWidth, shot.PixelHeight)
            ? shot
            : new CroppedBitmap(shot, new Int32Rect(keep.X, keep.Y, keep.Width, keep.Height));

        string file;
        try
        {
            file = CaptureFiles.NextScreenshot(Settings.ResolvedCaptureFolder(), CaptureFiles.Subject(CurrentUrl, _target), DateTimeOffset.Now);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (var fs = new FileStream(file, FileMode.CreateNew, FileAccess.Write)) encoder.Save(fs);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Status.Text = $"Could not save the screenshot: {ex.Message}";
            return;
        }
        var copied = Settings.CopyToClipboard && ClipboardService.CopyImage(image, file);
        Status.Text = $"Saved {file}{(copied ? " and copied it to the clipboard" : "")}.";
    }

    // ---------------------------------------------------------------- record

    /// <summary>Starts recording, or stops it, writes the video and puts the file on the clipboard.</summary>
    private async void OnRecord(object sender, RoutedEventArgs e)
    {
        if (_busyRecording) return;
        var capture = CaptureApi.Current;
        if (!capture.Enabled)
        {
            Status.Text = "Capture is not running yet.";
            return;
        }
        _busyRecording = true;
        RecordButton.IsEnabled = false;
        try
        {
            if (!capture.IsRecording)
            {
                await capture.StartRecordingAsync("browser", new RecordingOptions { Preset = CapturePreset.Trailer });
                Status.Text = capture.IsRecording ? "Recording… press ■ or Ctrl+Shift+R to stop." : "Recording could not start; see the capture log.";
                return;
            }
            Status.Text = "Writing the recording…";
            var r = await capture.StopRecordingAsync();
            if (!r.Succeeded)
            {
                Status.Text = $"Recording failed: {r.Error}";
                return;
            }
            var file = r.Files.FirstOrDefault(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) ?? r.PrimaryFile!;
            var copied = Settings.CopyToClipboard && ClipboardService.CopyFile(file);
            Status.Text = $"Saved {file} ({r.Duration.TotalSeconds:0.#}s){(copied ? " and copied it to the clipboard" : "")}.";
        }
        finally
        {
            _busyRecording = false;
            RecordButton.IsEnabled = true;
            UpdateCaptureState();
        }
    }

    private void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        var root = Settings.ResolvedCaptureFolder();
        var site = Path.Combine(root, CaptureFiles.Subject(CurrentUrl, _target));
        var folder = Directory.Exists(site) ? site : root;
        if (Directory.Exists(folder)) Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        else Status.Text = $"{folder} doesn't exist yet; take a screenshot first.";
    }

    private void UpdateCaptureState()
    {
        var options = App.CaptureHandle.Options;
        var capture = CaptureApi.Current;
        var recording = capture.Enabled && capture.IsRecording;
        RecordButton.Content = recording ? StopIcon : RecordIcon;
        RecordButton.Foreground = recording ? (Brush)FindResource("Rec") : (Brush)FindResource("Text");
        RecordButton.ToolTip = recording ? "Stop recording, save and copy (Ctrl+Shift+R)" : "Record (Ctrl+Shift+R)";
        if (options is null || !capture.Enabled)
        {
            CaptureState.Text = "";
            return;
        }
        var parts = new List<string>();
        if (options.SessionRecording) parts.Add("● SESSION");
        if (options.ScreenshotInterval is { } interval) parts.Add($"▣ every {interval.TotalSeconds:0.#}s");
        if (recording) parts.Add("● REC");
        CaptureState.Text = string.Join("   ", parts);
        CaptureState.Foreground = options.SessionRecording || recording ? (Brush)FindResource("Rec") : (Brush)FindResource("Muted");
    }

    // ---------------------------------------------------------------- favorites

    private void UpdateStar()
    {
        var starred = Settings.IsFavorite(CurrentUrl);
        StarButton.Content = starred ? StarFilledIcon : StarIcon;
        StarButton.Foreground = starred ? (Brush)FindResource("Star") : (Brush)FindResource("Text");
    }

    private void OnToggleStar(object sender, RoutedEventArgs e)
    {
        if (CurrentUrl is not { } url) return;
        if (Settings.IsFavorite(url)) Settings.Unstar(url, DateTimeOffset.Now);
        else Settings.Star(url, Web.CoreWebView2?.DocumentTitle, DateTimeOffset.Now);
        SaveSettings();
        UpdateStar();
    }

    private void OnStarMenu(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = StarButton, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
        var starred = Settings.IsFavorite(CurrentUrl);
        var toggle = new MenuItem { Header = starred ? "Remove this page from favorites" : "Add this page to favorites", InputGestureText = "Ctrl+D", IsEnabled = CurrentUrl is not null };
        toggle.Click += OnToggleStar;
        menu.Items.Add(toggle);
        menu.Items.Add(new Separator());
        var favorites = Settings.ActiveFavorites();
        if (favorites.Count == 0) menu.Items.Add(new MenuItem { Header = "No favorites yet", IsEnabled = false });
        foreach (var f in favorites)
        {
            var item = new MenuItem { Header = f.Title.Length > 70 ? f.Title[..70] + "…" : f.Title, ToolTip = f.Url };
            var url = f.Url;
            item.Click += (_, _) => Navigate(url);
            menu.Items.Add(item);
        }
        menu.IsOpen = true;
    }

    // ---------------------------------------------------------------- settings

    private void OnSettings(object sender, RoutedEventArgs e)
    {
        var folderBefore = Settings.CaptureFolder;
        if (new SettingsWindow(Settings, App.SettingsStore.FilePath, this).ShowDialog() != true) return;
        SaveSettings();
        Status.Text = Settings.CaptureFolder != folderBefore
            ? "Settings saved. Screenshots use the new folder now; recordings use it from the next launch."
            : "Settings saved.";
    }

    private void SaveSettings()
    {
        try
        {
            App.SettingsStore.Save(Settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Status.Text = $"Could not save settings: {ex.Message}";
        }
    }
}
