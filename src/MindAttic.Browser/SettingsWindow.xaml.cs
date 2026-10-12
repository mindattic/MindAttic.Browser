using System.Windows;
using Microsoft.Win32;

namespace MindAttic.Browser;

/// <summary>The cog's dialog: edits a copy of the settings and saves it through Vault on Save.</summary>
public partial class SettingsWindow : Window
{
    private readonly BrowserSettings _settings;

    internal SettingsWindow(BrowserSettings settings, string storePath, Window owner)
    {
        InitializeComponent();
        Owner = owner;
        _settings = settings;
        HomePage.Text = settings.HomePage;
        SearchUrl.Text = settings.SearchUrl;
        CaptureFolder.Text = settings.CaptureFolder;
        CopyToClipboard.IsChecked = settings.CopyToClipboard;
        CropScreenshots.IsChecked = settings.CropScreenshots;
        StorePath.Text = "Saved to " + storePath;
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Capture folder", InitialDirectory = CaptureFolder.Text };
        if (dialog.ShowDialog(this) == true) CaptureFolder.Text = dialog.FolderName;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (!SearchUrl.Text.Contains("{query}", StringComparison.Ordinal))
        {
            MessageBox.Show(this, "The search URL needs {query} where the search text goes, e.g. https://www.google.com/search?q={query}",
                "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _settings.HomePage = string.IsNullOrWhiteSpace(HomePage.Text) ? BrowserSettings.DefaultHomePage : HomePage.Text.Trim();
        _settings.SearchUrl = SearchUrl.Text.Trim();
        _settings.CaptureFolder = string.IsNullOrWhiteSpace(CaptureFolder.Text) ? BrowserSettings.DefaultCaptureFolder : CaptureFolder.Text.Trim();
        _settings.CopyToClipboard = CopyToClipboard.IsChecked == true;
        _settings.CropScreenshots = CropScreenshots.IsChecked == true;
        DialogResult = true;
    }
}
